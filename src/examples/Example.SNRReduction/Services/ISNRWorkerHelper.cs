using System.IO;
using AlsaSharp;

namespace Example.SNRReduction.Services;

public interface ISNRWorkerHelper
{
    MemoryStream BuildInlineSineWav(SoundDeviceSettings settings, double frequencyHz, double durationSeconds);
    float[] ReadWavToMonoFloat(Stream wavStream);
    string SanitizeFileName(string s);
    Accumulator CreateAccumulator(ISoundDevice device);

    public class Accumulator
    {
        private readonly ISoundDevice _device;
        public List<long> SumSq = new List<long>();
        public int Samples;
        private bool _headerSeen;

        public Accumulator(ISoundDevice device)
        {
            _device = device;
            SumSq = new List<long>();
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
            while (SumSq.Count < channels) SumSq.Add(0);

            for (int i = 0; i < frameCount; i++)
            {
                int offset = i * channels * bytesPerSample;
                for (int ch = 0; ch < channels; ch++)
                {
                    int so = offset + ch * bytesPerSample;
                    if (so + bytesPerSample - 1 >= buffer.Length) break;
                    long sample = 0;
                    if (bytesPerSample == 3)
                    {
                        int v = buffer[so] | (buffer[so + 1] << 8) | (buffer[so + 2] << 16);
                        if ((v & 0x800000) != 0) v |= unchecked((int)0xFF000000);
                        sample = v;
                    }
                    else if (bytesPerSample == 4)
                    {
                        sample = BitConverter.ToInt32(buffer, so);
                    }
                    else
                    {
                        sample = BitConverter.ToInt16(buffer, so);
                    }
                    SumSq[ch] += sample * sample;
                }
                Samples++;
            }
        }

        public (List<double> ChannelDbfs, List<double> ChannelRms) ComputeResults()
        {
            if (Samples == 0)
            {
                int chs = (int)(_device?.Settings?.RecordingChannels ?? (uint)1);
                var dbfs = Enumerable.Repeat(-120.0, chs).ToList();
                var rms = Enumerable.Repeat(0.0, chs).ToList();
                return (dbfs, rms);
            }

            int deviceChannels = (int)(_device?.Settings?.RecordingChannels ?? (uint)2);
            int bits = (int)(_device?.Settings?.RecordingBitsPerSample ?? (uint)16);
            double maxAmp = Math.Pow(2.0, bits - 1) - 1.0;

            var sumSq = SumSq ?? new List<long>();
            while (sumSq.Count < deviceChannels) sumSq.Add(0);

            var channelRms = new List<double>(deviceChannels);
            var channelDbfs = new List<double>(deviceChannels);
            for (int ch = 0; ch < deviceChannels; ch++)
            {
                double rms = Math.Sqrt((double)sumSq[ch] / (double)Samples) / maxAmp;
                channelRms.Add(rms);
                channelDbfs.Add(rms <= 0 ? -120.0 : 20.0 * Math.Log10(rms));
            }

            return (channelDbfs, channelRms);
        }
    }
}
