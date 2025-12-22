using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AlsaSharp;
using AlsaSharp.Library;

namespace Example.SNRReduction.Services
{
    /// <summary>
    /// Simple diagnostic helper: record a WAV from an ISoundDevice and compute per-channel RMS and dBFS.
    /// </summary>
    public class AudioDiagnosticService
    {
        /// <summary>
        /// Record from the provided device for <paramref name="seconds"/> into <paramref name="outPath"/>
        /// and return per-channel diagnostic results.
        /// </summary>
        public DiagnosticResult AnalyzeDevice(ISoundDevice device, uint seconds, string outPath)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (string.IsNullOrWhiteSpace(outPath))
                throw new ArgumentNullException(nameof(outPath));

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(outPath) ?? ".");

            // Record using the device (blocking call provided by ISoundDevice)
            device.Record(seconds, outPath);

            // Open recorded file and analyze
            using var fs = File.OpenRead(outPath);
            var header = WavHeader.FromStream(fs);

            int channels = (int)header.NumChannels;
            int bits = (int)header.BitsPerSample;
            int bytesPerSample = Math.Max(1, bits / 8);
            int blockAlign = (int)header.BlockAlign;
            long dataBytes = header.Subchunk2Size;
            long frames = dataBytes / blockAlign;

            var sumSq = new double[channels];
            long frameCount = 0;

            // Read in reasonably sized chunks
            var buffer = new byte[8192 * blockAlign];
            int read;
            while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
            {
                int framesInBuffer = read / blockAlign;
                for (int f = 0; f < framesInBuffer; f++)
                {
                    int frameOffset = f * blockAlign;
                    for (int ch = 0; ch < channels; ch++)
                    {
                        int sampleOffset = frameOffset + ch * bytesPerSample;
                        long sample = 0;
                        if (bytesPerSample == 1)
                        {
                            sample = (sbyte)buffer[sampleOffset];
                        }
                        else if (bytesPerSample == 2)
                        {
                            sample = BitConverter.ToInt16(buffer, sampleOffset);
                        }
                        else if (bytesPerSample == 3)
                        {
                            // 24-bit little endian -> sign extend to 32-bit
                            int b0 = buffer[sampleOffset];
                            int b1 = buffer[sampleOffset + 1];
                            int b2 = buffer[sampleOffset + 2];
                            int v = (b0) | (b1 << 8) | (b2 << 16);
                            // sign extend
                            if ((v & 0x800000) != 0)
                                v |= unchecked((int)0xFF000000);
                            sample = v;
                        }
                        else if (bytesPerSample == 4)
                        {
                            sample = BitConverter.ToInt32(buffer, sampleOffset);
                        }
                        else
                        {
                            // unsupported width
                            throw new NotSupportedException($"Unsupported sample width: {bytesPerSample} bytes");
                        }

                        sumSq[ch] += (double)sample * (double)sample;
                    }
                    frameCount++;
                }
            }

            var results = new DiagnosticResult
            {
                FilePath = outPath,
                SampleRate = header.SampleRate,
                BitsPerSample = header.BitsPerSample,
                Channels = channels,
                Frames = frameCount,
                ChannelDbfs = new double[channels],
                ChannelRms = new double[channels]
            };

            double maxAmp = Math.Pow(2.0, bits - 1) - 1.0;
            for (int ch = 0; ch < channels; ch++)
            {
                if (frameCount == 0)
                {
                    results.ChannelRms[ch] = 0.0;
                    results.ChannelDbfs[ch] = double.NegativeInfinity;
                }
                else
                {
                    double meanSq = sumSq[ch] / (double)frameCount;
                    double rms = Math.Sqrt(meanSq) / maxAmp;
                    results.ChannelRms[ch] = rms;
                    results.ChannelDbfs[ch] = rms <= 0 ? double.NegativeInfinity : 20.0 * Math.Log10(rms);
                }
            }

            return results;
        }
    }

    public class DiagnosticResult
    {
        public string FilePath { get; set; } = string.Empty;
        public uint SampleRate { get; set; }
        public ushort BitsPerSample { get; set; }
        public int Channels { get; set; }
        public long Frames { get; set; }
        public double[] ChannelRms { get; set; } = Array.Empty<double>();
        public double[] ChannelDbfs { get; set; } = Array.Empty<double>();
    }
}
