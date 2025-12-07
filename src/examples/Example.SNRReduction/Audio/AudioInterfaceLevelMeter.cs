using AlsaSharp;
using AlsaSharp.Library.Logging;
using Example.SNRReduction.Services;

namespace Example.SNRReduction.Audio;

public class AudioInterfaceLevelMeter(ISoundDevice device, ILog<AudioInterfaceLevelMeter> log) : IAudioInterfaceLevelMeter
{
    private const double noiseFloor = -120.0;
    private readonly ISoundDevice _device = device;
    private readonly ILog<AudioInterfaceLevelMeter> _log = log;
    private readonly object _recordLock = new object();
    /// <summary>
    /// Measure per-channel RMS and return dBFS values for left and right channels.
    /// </summary>
    public (double LeftDbfs, double RightDbfs) MeasureLevels(int captureDurationMs)
    {
        var sumSqL = 0L; var sumSqR = 0L; int samples = 0;
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(captureDurationMs + 1000));
        bool headerSeen = false;

        void OnData(byte[] buffer)
        {
            if (!headerSeen) { headerSeen = true; return; }
            int bytesPerSample = 2, channels = 2;
            int frameCount = buffer.Length / (bytesPerSample * channels);
            if (frameCount <= 0) return;
            for (int i = 0; i < frameCount; i++)
            {
                int offset = i * channels * bytesPerSample;
                if (offset + 3 >= buffer.Length) break;
                short sL = BitConverter.ToInt16(buffer, offset);
                short sR = BitConverter.ToInt16(buffer, offset + 2);
                sumSqL += (long)sL * sL; sumSqR += (long)sR * sR; samples++;
            }
        }

        Task? task = null;
        try
        {
            lock (_recordLock)
            {
                task = Task.Run(() => _device.Record(OnData, cts.Token));
                task.Wait(cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // expected when token cancels; avoid spamming logs with expected cancellations
        }
        catch (AlsaSharp.Internal.AlsaDeviceException ex)
        {
            _log?.Warn($"Recording task failed: {ex.Message}");
        }
        catch (AggregateException ae)
        {
            _log?.Warn($"Recording task aggregate failure: {ae.InnerException?.Message}");
        }
        catch (Exception ex)
        {
            _log?.Warn($"Recording unexpected failure: {ex.Message}");
        }

        if (samples == 0)
        {
            // No data captured — return a sensible floor instead of -Infinity so callers can visualise.
            return (noiseFloor, noiseFloor);
        }

        double rmsL = Math.Sqrt(sumSqL / (double)samples) / 32768.0;
        double rmsR = Math.Sqrt(sumSqR / (double)samples) / 32768.0;

        double leftDbfs = rmsL <= 0 ? noiseFloor : 20.0 * Math.Log10(rmsL);
        double rightDbfs = rmsR <= 0 ? noiseFloor : 20.0 * Math.Log10(rmsR);
        return (leftDbfs, rightDbfs);
    }

    
}
