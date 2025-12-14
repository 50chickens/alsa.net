using AlsaSharp;
using AlsaSharp.Library.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Example.SNRReduction;

public class SNRTools
{
    private readonly ILog<SNRTools> _log;

    public SNRTools(ILog<SNRTools> log)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    private byte[] GenerateToneWav(int sampleRate, int channels, int bitsPerSample, double freq, int seconds, double amplitude)
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
        if (device is null)
        {
            _log.Warn("MeasureNoise called with null device");
            return 0.0;
        }

        if (seconds <= 0)
        {
            _log.Warn($"MeasureNoise called with non-positive seconds: {seconds}");
            return 0.0;
        }

        var rmsList = new List<double>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds + 1));
        bool headerSeen = false;

        void OnData(byte[] buffer)
        {
            if (buffer is null || buffer.Length == 0) return;
            if (!headerSeen) { headerSeen = true; return; }
            const int bytesPerSample = 2; const int channels = 2;
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

        var recordTask = Task.Run(() => device.Record(OnData, cts.Token), cts.Token);
        _ = recordTask.ContinueWith(t => { var ex = t.Exception ?? new Exception("record task faulted"); _log.Error(ex, "MeasureNoise: record task faulted"); }, TaskContinuationOptions.OnlyOnFaulted);

        var finished = Task.WhenAny(recordTask, Task.Delay(TimeSpan.FromSeconds(seconds + 1))).GetAwaiter().GetResult();
        if (finished != recordTask)
        {
            cts.Cancel();
            _log.Info("MeasureNoise: timeout reached, recording canceled");
        }

        if (recordTask.IsFaulted)
        {
            _log.Error(recordTask.Exception, $"MeasureNoise: recording failed: {recordTask.Exception?.Flatten().Message}");
            return 0.0;
        }

        return rmsList.Count == 0 ? 0.0 : rmsList.Average();
    }

    public async Task<double> MeasureSignalAsync(ISoundDevice device, int seconds, double freq)
    {
        if (device is null)
        {
            _log.Warn("MeasureSignalAsync called with null device");
            return 0.0;
        }

        if (seconds <= 0)
        {
            _log.Warn($"MeasureSignalAsync called with non-positive seconds: {seconds}");
            return 0.0;
        }

        if (freq <= 0)
        {
            _log.Warn($"MeasureSignalAsync called with non-positive frequency: {freq}");
            return 0.0;
        }

        int sampleRate = 48000, channels = 2, bits = 16;
        var tone = GenerateToneWav(sampleRate, channels, bits, freq, seconds, 0.5);
        var rmsList = new List<double>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds + 2));
        bool headerSeen = false;

        void OnData(byte[] buffer)
        {
            if (buffer is null || buffer.Length == 0) return;
            if (!headerSeen) { headerSeen = true; return; }
            const int bytesPerSample = 2, chs = 2;
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

        var recordTask = Task.Run(() => device.Record(OnData, cts.Token), cts.Token);
        _ = recordTask.ContinueWith(t => { var ex = t.Exception ?? new Exception("record task faulted"); _log.Error(ex, "MeasureSignalAsync: record task faulted"); }, TaskContinuationOptions.OnlyOnFaulted);

        // Play and wait for recording to complete or timeout
        device.Play(new MemoryStream(tone), cts.Token);

        var finished = await Task.WhenAny(recordTask, Task.Delay(TimeSpan.FromSeconds(seconds + 2)));
        if (finished != recordTask)
        {
            cts.Cancel();
            _log.Info("MeasureSignalAsync: recording timed out and was canceled");
        }

        if (recordTask.IsFaulted)
        {
            _log.Error(recordTask.Exception, $"MeasureSignalAsync: recording failed: {recordTask.Exception?.Flatten().Message}");
            return 0.0;
        }

        return rmsList.Count == 0 ? 0.0 : rmsList.Average();
    }

    internal void Initialize()
    {
        _log.Info("***** NR Tools initialized.");
    }
}