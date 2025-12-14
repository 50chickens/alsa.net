using AlsaSharp;
using AlsaSharp.Library;
using AlsaSharp.Library.Logging;
using Example.SNRReduction.Services;

namespace Example.SNRReduction.Audio;

public class AudioInterfaceLevelMeter(ISoundDevice device, ILog<AudioInterfaceLevelMeter> log) : IAudioInterfaceLevelMeter
{
    private const double noiseFloor = -120.0;
    private readonly ISoundDevice _device = device;
    private readonly ILog<AudioInterfaceLevelMeter> _log = log;
    private readonly object _recordLock = new object();
    public (double LeftDbfs, double RightDbfs) MeasureLevels(int captureDurationMs)
    {
        var sumSqL = 0L; var sumSqR = 0L; int samples = 0;
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(captureDurationMs + 1000));
        var acc = new Accumulator(_device);

        Task? task = null;
        try
        {
                lock (_recordLock)
                {
                    task = Task.Run(() => _device.Record(acc.OnData, cts.Token));
                    task.Wait(cts.Token);
                }
        }
        catch (OperationCanceledException)
        {
            // expected when token cancels; avoid spamming logs with expected cancellations
        }
        catch (AlsaDeviceException ex)
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

        if (acc.Samples == 0)
        {
            // No data captured — return a sensible floor instead of -Infinity so callers can visualise.
            return (noiseFloor, noiseFloor);
        }

        // Determine channels from device settings and compute accordingly
        int deviceChannels = (int)(_device?.Settings?.RecordingChannels ?? (uint)2);
        int bits = (int)(_device?.Settings?.RecordingBitsPerSample ?? (uint)16);
        double maxAmp = Math.Pow(2.0, bits - 1) - 1.0; // e.g., 32767 for 16-bit

        double rmsL = Math.Sqrt(acc.SumSqL / (double)acc.Samples) / maxAmp;
        double leftDbfs = rmsL <= 0 ? noiseFloor : 20.0 * Math.Log10(rmsL);

        if (deviceChannels <= 1)
        {
            _log?.Info("Input device is mono; skipping right-channel measurement.");
            return (leftDbfs, double.NaN);
        }
        double rmsR = Math.Sqrt(acc.SumSqR / (double)acc.Samples) / maxAmp;
        double rightDbfs = rmsR <= 0 ? noiseFloor : 20.0 * Math.Log10(rmsR);
        return (leftDbfs, rightDbfs);
    }

    private class Accumulator
    {
        private readonly ISoundDevice _device;
        public long SumSqL;
        public long SumSqR;
        public int Samples;
        private bool _headerSeen;

        public Accumulator(ISoundDevice device)
        {
            _device = device;
            SumSqL = 0;
            SumSqR = 0;
            Samples = 0;
            _headerSeen = false;
        }

        public void OnData(byte[] buffer)
        {
            if (!_headerSeen) { _headerSeen = true; return; }

            int bitsPerSample = (int)(_device?.Settings?.RecordingBitsPerSample ?? (uint)16);
            int bytesPerSample = Math.Max(1, bitsPerSample / 8);
            int channels = (int)(_device?.Settings?.RecordingChannels ?? (uint)2);

            if (channels <= 0) channels = 1;

            int frameCount = buffer.Length / (bytesPerSample * channels);
            if (frameCount <= 0) return;
            for (int i = 0; i < frameCount; i++)
            {
                int offset = i * channels * bytesPerSample;
                // ensure we have enough bytes for at least one sample
                if (offset + bytesPerSample - 1 >= buffer.Length) break;

                // read left channel (currently only 16-bit signed samples are supported)
                short sL = BitConverter.ToInt16(buffer, offset);
                SumSqL += (long)sL * sL;

                if (channels >= 2)
                {
                    // ensure right sample bytes exist
                    if (offset + bytesPerSample * 2 - 1 >= buffer.Length) break;
                    short sR = BitConverter.ToInt16(buffer, offset + bytesPerSample);
                    SumSqR += (long)sR * sR;
                }

                Samples++;
            }
        }
    }
}
