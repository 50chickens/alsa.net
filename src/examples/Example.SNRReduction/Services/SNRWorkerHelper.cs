using System.IO;
using AlsaSharp;

namespace Example.SNRReduction.Services;

public class SNRWorkerHelper : ISNRWorkerHelper
{
    public SNRWorkerHelper()
    {
    }

    public MemoryStream BuildInlineSineWav(SoundDeviceSettings settings, double frequencyHz, double durationSeconds)
    {
        int sampleRate = (int)(settings.RecordingSampleRate > 0 ? settings.RecordingSampleRate : 48000);
        int channels = Math.Max(1, (int)settings.RecordingChannels);
        int bits = Math.Max(16, (int)settings.RecordingBitsPerSample);
        int bytesPerSample = bits / 8;
        int totalFrames = (int)(sampleRate * durationSeconds);

        var header = AlsaSharp.Library.WavHeader.Build((uint)sampleRate, (ushort)channels, (ushort)bits);
        var ms = new MemoryStream();
        header.WriteToStream(ms);

        double amplitude = 0.5 * (Math.Pow(2, bits - 1) - 1);
        for (int i = 0; i < totalFrames; i++)
        {
            double t = (double)i / sampleRate;
            double v = amplitude * Math.Sin(2.0 * Math.PI * frequencyHz * t);
            for (int ch = 0; ch < channels; ch++)
            {
                if (bytesPerSample == 2)
                {
                    short s = (short)Math.Round(v);
                    ms.WriteByte((byte)(s & 0xFF));
                    ms.WriteByte((byte)((s >> 8) & 0xFF));
                }
                else if (bytesPerSample == 3)
                {
                    int iv = (int)Math.Round(v);
                    ms.WriteByte((byte)(iv & 0xFF));
                    ms.WriteByte((byte)((iv >> 8) & 0xFF));
                    ms.WriteByte((byte)((iv >> 16) & 0xFF));
                }
                else
                {
                    int iv = (int)Math.Round(v);
                    ms.WriteByte((byte)(iv & 0xFF));
                    ms.WriteByte((byte)((iv >> 8) & 0xFF));
                    ms.WriteByte((byte)((iv >> 16) & 0xFF));
                    ms.WriteByte((byte)((iv >> 24) & 0xFF));
                }
            }
        }

        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }

    public float[] ReadWavToMonoFloat(Stream wavStream)
    {
        var header = AlsaSharp.Library.WavHeader.FromStream(wavStream);
        int bytesPerSample = Math.Max(1, header.BitsPerSample / 8);
        int channels = Math.Max(1, (int)header.NumChannels);
        int totalBytes = (int)header.Subchunk2Size;
        long remaining = 0;
        if (wavStream.CanSeek)
        {
            remaining = wavStream.Length - wavStream.Position;
        }
        if (header.Subchunk2Size == 0xFFFFFFFFu || totalBytes <= 0 || (remaining > 0 && totalBytes > remaining))
        {
            if (!wavStream.CanSeek)
            {
                return Array.Empty<float>();
            }
            totalBytes = (int)Math.Max(0, Math.Min(remaining, int.MaxValue));
        }

        if (totalBytes == 0) return Array.Empty<float>();

        byte[] data = new byte[totalBytes];
        int read = wavStream.Read(data, 0, totalBytes);
        if (read != totalBytes) Array.Resize(ref data, Math.Max(0, read));

        int frames = data.Length / (bytesPerSample * channels);
        var mono = new float[frames];

        double maxAmp = Math.Pow(2.0, header.BitsPerSample - 1) - 1.0;

        for (int f = 0; f < frames; f++)
        {
            double acc = 0.0;
            for (int ch = 0; ch < channels; ch++)
            {
                int offset = f * channels * bytesPerSample + ch * bytesPerSample;
                double sample = 0.0;
                if (bytesPerSample == 2) sample = BitConverter.ToInt16(data, offset);
                else if (bytesPerSample == 3)
                {
                    int v = data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16);
                    if ((v & 0x800000) != 0) v |= unchecked((int)0xFF000000);
                    sample = v;
                }
                else if (bytesPerSample == 4) sample = BitConverter.ToInt32(data, offset);
                else sample = data[offset];
                acc += sample / maxAmp;
            }
            mono[f] = (float)(acc / channels);
        }

        return mono;
    }

    public string SanitizeFileName(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "unknown";
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Replace(' ', '_');
    }

    public ISNRWorkerHelper.Accumulator CreateAccumulator(ISoundDevice device)
    {
        return new ISNRWorkerHelper.Accumulator(device);
    }
}
