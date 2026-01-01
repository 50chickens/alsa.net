#nullable enable

using AlsaSharp;
using AlsaSharp.Library;
using AlsaSharp.Library.Logging;
using Example.SNRReduction.Services;

namespace Example.SNRReduction.Audio;

public class AudioInterfaceLevelMeter(ILog<AudioInterfaceLevelMeter> log) : IAudioInterfaceLevelMeterService
{
    private const double noiseFloor = -150.0; //silence is ~ 90 dBFS, use -150 dBFS as noise floor
    private readonly ILog<AudioInterfaceLevelMeter> _log = log;
    private readonly object _recordLock = new object();
    
    public (List<double> ChannelDbfs, List<double> ChannelRms) MeasureLevels(ISoundDevice device, int captureDurationMs, CancellationToken cancellationToken)
    {
        var acc = new Accumulator(device);
        
        // Create a timeout cancellation source that respects the capture duration
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(captureDurationMs);
        
        try
        {
            lock (_recordLock)
            {
                RecordAudioToAccumulator(device, acc, timeoutCts);
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
            // No data captured — return noise floor per channel
            int chs = (int)(device?.Settings?.RecordingChannels ?? (uint)1);
            var dbfs = Enumerable.Repeat(noiseFloor, chs).ToList();
            var rms = Enumerable.Repeat(0.0, chs).ToList();
            return (dbfs, rms);
        }

        int deviceChannels = (int)(device?.Settings?.RecordingChannels ?? (uint)2);
        int bits = (int)(device?.Settings?.RecordingBitsPerSample ?? (uint)16);
        double maxAmp = Math.Pow(2.0, bits - 1) - 1.0; // e.g., 32767 for 16-bit

        // use accumulated sums from Accumulator
        var sumSq = acc.SumSq ?? new List<long>();
        while (sumSq.Count < deviceChannels)
            sumSq.Add(0);

        var channelRms = new List<double>(deviceChannels);
        var channelDbfs = new List<double>(deviceChannels);
        for (int ch = 0; ch < deviceChannels; ch++)
        {
            // RMS calculation: divide by number of samples PER CHANNEL, not total frames
            double rms = Math.Sqrt((double)sumSq[ch] / (double)acc.Samples) / maxAmp;
            channelRms.Add(rms);
            channelDbfs.Add(rms <= 0 ? noiseFloor : 20.0 * Math.Log10(rms));
        }

        return (channelDbfs, channelRms);
    }
    private void RecordAudioToAccumulator(ISoundDevice device, Accumulator acc, CancellationTokenSource timeoutCts) =>  device.Record(acc.OnData, timeoutCts.Token);
    
}
