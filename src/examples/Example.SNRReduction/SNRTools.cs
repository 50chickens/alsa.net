using AlsaSharp;
using AlsaSharp.Library.Logging;

namespace Example.SNRReduction;

public class SNRTools(ILog<SNRTools> log)
    {
        private readonly ILog<SNRTools> _log = log;
        byte[] GenerateToneWav(int sampleRate, int channels, int bitsPerSample, double freq, int seconds, double amplitude)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            int byteRate = sampleRate * channels * bitsPerSample / 8;
            int blockAlign = channels * bitsPerSample / 8;
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + sampleRate * seconds * blockAlign);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1);
            bw.Write((short)channels);
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write((short)blockAlign);
            bw.Write((short)bitsPerSample);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            int dataSize = sampleRate * seconds * blockAlign;
            bw.Write(dataSize);
            int totalFrames = sampleRate * seconds;
            for (int i = 0; i < totalFrames; i++)
            {
                double t = i / (double)sampleRate;
                double s = Math.Sin(2.0 * Math.PI * freq * t) * amplitude;
                short sample = (short)(s * short.MaxValue);
                for (int ch = 0; ch < channels; ch++) bw.Write(sample);
            }
            return ms.ToArray();
        }

        internal double MeasureNoise(ISoundDevice device, int seconds)
        {
            var rmsList = new List<double>();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds + 1));
            bool headerSeen = false;

            void OnData(byte[] buffer)
            {
                if (!headerSeen) { headerSeen = true; return; }
                int bytesPerSample = 2, channels = 2;
                int frameCount = buffer.Length / (bytesPerSample * channels);
                if (frameCount <= 0) return;
                long sumSqL = 0, sumSqR = 0; int samples = 0;
                for (int i = 0; i < frameCount; i++)
                {
                    int offset = i * channels * bytesPerSample;
                    if (offset + 3 >= buffer.Length) break;
                    short sL = BitConverter.ToInt16(buffer, offset);
                    short sR = BitConverter.ToInt16(buffer, offset + 2);
                    sumSqL += (long)sL * sL; sumSqR += (long)sR * sR; samples++;
                }
                if (samples == 0) return;
                double rms = Math.Sqrt((sumSqL + sumSqR) / (double)(samples * 2)) / 32768.0;
                rmsList.Add(rms);
            }

            var task = Task.Run(() => device.Record(OnData, cts.Token));
            try { task.Wait(cts.Token); } catch { }
            return rmsList.Count == 0 ? 0.0 : rmsList.Average();
        }

        public async Task<double> MeasureSignalAsync(ISoundDevice device, int seconds, double freq)
        {
            int sampleRate = 48000, channels = 2, bits = 16;
            var tone = GenerateToneWav(sampleRate, channels, bits, freq, seconds, 0.5);
            var rmsList = new List<double>();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds + 2));
            bool headerSeen = false;

            void OnData(byte[] buffer)
            {
                if (!headerSeen) { headerSeen = true; return; }
                int bytesPerSample = 2, chs = 2;
                int frameCount = buffer.Length / (bytesPerSample * chs);
                if (frameCount <= 0) return;
                long sumSqL = 0, sumSqR = 0; int samples = 0;
                for (int i = 0; i < frameCount; i++)
                {
                    int offset = i * chs * bytesPerSample;
                    if (offset + 3 >= buffer.Length) break;
                    short sL = BitConverter.ToInt16(buffer, offset);
                    short sR = BitConverter.ToInt16(buffer, offset + 2);
                    sumSqL += (long)sL * sL; sumSqR += (long)sR * sR; samples++;
                }
                if (samples == 0) return;
                double rms = Math.Sqrt((sumSqL + sumSqR) / (double)(samples * 2)) / 32768.0;
                rmsList.Add(rms);
            }

            var recordTask = Task.Run(() => device.Record(OnData, cts.Token));
            device.Play(new MemoryStream(tone), cts.Token);
            try { await recordTask; } catch { }
            return rmsList.Count == 0 ? 0.0 : rmsList.Average();
        }

    internal void Initialize()
    {
        _log.Info("***** NR Tools initialized.");         
    }
}