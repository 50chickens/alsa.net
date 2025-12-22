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
        Task? task = null;
        
        // Create a timeout cancellation source that respects the capture duration
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(captureDurationMs);
        
        try
        {
            lock (_recordLock)
            {
                task = Task.Run(() => device.Record(acc.OnData, timeoutCts.Token), timeoutCts.Token);
                task.Wait(timeoutCts.Token);
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

    private class Accumulator
    {
        private readonly ISoundDevice _device;
        public List<long> SumSq = new List<long>();
        public int Samples; // This is the count of samples PER CHANNEL

        public Accumulator(ISoundDevice device)
        {
            _device = device;
            SumSq = new List<long>();
            Samples = 0;
        }

        public void OnData(byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0)
                return;

            int bitsPerSample = (int)(_device?.Settings?.RecordingBitsPerSample ?? (uint)16);
            int bytesPerSample = Math.Max(1, bitsPerSample / 8);
            int channels = (int)(_device?.Settings?.RecordingChannels ?? (uint)2);

            if (channels <= 0)
                channels = 1;

            int frameCount = buffer.Length / (bytesPerSample * channels);
            if (frameCount <= 0)
                return;
            
            // ensure SumSq list capacity
            while (SumSq.Count < channels)
                SumSq.Add(0);
            
            for (int i = 0; i < frameCount; i++)
            {
                int offset = i * channels * bytesPerSample;
                if (offset + (channels * bytesPerSample) > buffer.Length)
                    break;

                // read all channels generically
                for (int ch = 0; ch < channels; ch++)
                {
                    int so = offset + ch * bytesPerSample;
                    if (so + bytesPerSample > buffer.Length)
                        break;
                    
                    long sample = 0;
                    if (bytesPerSample == 3)
                    {
                        // 24-bit signed integer (little-endian)
                        int v = buffer[so] | (buffer[so + 1] << 8) | (buffer[so + 2] << 16);
                        if ((v & 0x800000) != 0)
                            v |= unchecked((int)0xFF000000);
                        sample = v;
                    }
                    else if (bytesPerSample == 4)
                    {
                        sample = BitConverter.ToInt32(buffer, so);
                    }
                    else // bytesPerSample == 2 or 1
                    {
                        sample = BitConverter.ToInt16(buffer, so);
                    }
                    
                    SumSq[ch] += sample * sample;
                }
            }
            
            // Samples represents the number of samples per channel
            Samples += frameCount;
        }
    }
}
