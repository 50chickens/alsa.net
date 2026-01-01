#nullable enable

using AlsaSharp;
using AlsaSharp.Library;
using AlsaSharp.Library.Logging;
using Example.SNRReduction.Services;

namespace Example.SNRReduction.Audio;

public class AlsaLoopbackTestService(ILog<AlsaLoopbackTestService> log) : IAlsaLoopbackTestService
{
    private const double noiseFloor = -150.0;
    private readonly ILog<AlsaLoopbackTestService> _log = log;

    public (AlsaLoopbackTestResult PlayedSignal, AlsaLoopbackTestResult RecordedSignal, bool IsLoopbackWorking) TestLoopback(
        ISoundDevice playbackDevice,
        ISoundDevice recordingDevice,
        int testDurationMs = 3000)
    {
        _log.Info($"=== ALSA Loopback Test ===");
        _log.Info($"Test duration: {testDurationMs}ms");

        // Generate a test tone WAV file in memory (1000 Hz sine wave)
        var testToneStream = GenerateTestToneWav(1000, 48000, testDurationMs / 1000.0);
        _log.Info($"Generated test tone: 1000Hz sine wave");

        // Create accumulator for recorded signal
        var recordedAccumulator = new AudioAccumulator(recordingDevice);

        // Start playback and recording simultaneously
        using var cts = new CancellationTokenSource(testDurationMs + 2000); // Add 2 second buffer
        var playTask = Task.Run(() =>
        {
            try
            {
                _log.Info("Starting playback...");
                testToneStream.Seek(0, SeekOrigin.Begin);
                playbackDevice.Play(testToneStream, cts.Token);
                _log.Info("Playback completed");
            }
            catch (OperationCanceledException)
            {
                _log.Info("Playback cancelled as expected");
            }
            catch (Exception ex)
            {
                _log.Error($"Playback error: {ex.Message}");
            }
        });

        var recordTask = Task.Run(() =>
        {
            try
            {
                _log.Info("Starting recording...");
                recordingDevice.Record(recordedAccumulator.OnData, cts.Token);
                _log.Info("Recording completed");
            }
            catch (OperationCanceledException)
            {
                _log.Info("Recording cancelled as expected");
            }
            catch (Exception ex)
            {
                _log.Error($"Recording error: {ex.Message}");
            }
        });

        try
        {
            Task.WaitAll(playTask, recordTask);
        }
        catch (Exception ex)
        {
            _log.Warn($"Error waiting for tasks: {ex.Message}");
        }

        // Analyze recorded signal (what we captured)
        var recordedResult = AnalyzeSignal(recordedAccumulator, recordingDevice, "Recorded Signal");

        // Determine if loopback is working
        // Loopback is working if recorded signal has reasonable amplitude
        bool isLoopbackWorking = recordedResult.HasSignal && (recordedResult.PeakAmplitude.Any(p => p > 0.001));

        _log.Info($"\n--- Loopback Test Results ---");
        _log.Info($"Recorded Signal: {recordedResult}");
        _log.Info($"Loopback Status: {(isLoopbackWorking ? "WORKING ✓" : "NOT WORKING ✗")}");

        if (!isLoopbackWorking && recordedResult.Samples > 0)
        {
            _log.Error($"⚠ Hardware loopback not detected! Recorded {recordedResult.Samples} samples but with no significant signal.");
            _log.Error($"  Check: Cable connections, ALSA mixer routing, ADC input levels");
        }

        // Create a dummy result for played signal (we don't actually capture it, but report what we played)
        var playedResult = new AlsaLoopbackTestResult
        {
            Samples = (int)(48000 * (testDurationMs / 1000.0)),
            ChannelDbfs = new() { -3.0, -3.0 },
            ChannelRms = new() { 0.5, 0.5 },
            PeakAmplitude = new() { 0.5, 0.5 },
            SignalToNoiseRatio = 147.0,
            HasSignal = true
        };

        return (playedResult, recordedResult, isLoopbackWorking);
    }

    private AlsaLoopbackTestResult AnalyzeSignal(AudioAccumulator accumulator, ISoundDevice device, string label)
    {
        var result = new AlsaLoopbackTestResult
        {
            Samples = accumulator.Samples
        };

        if (accumulator.Samples == 0)
        {
            _log.Warn($"{label}: No samples captured");
            return result;
        }

        int deviceChannels = (int)(device?.Settings?.RecordingChannels ?? (uint)2);
        int bits = (int)(device?.Settings?.RecordingBitsPerSample ?? (uint)32);
        double maxAmp = Math.Pow(2.0, bits - 1) - 1.0;

        var sumSq = accumulator.SumSq ?? new List<long>();
        var peakValues = accumulator.PeakValues ?? new List<double>();

        // Ensure lists are properly sized
        while (sumSq.Count < deviceChannels)
            sumSq.Add(0);
        while (peakValues.Count < deviceChannels)
            peakValues.Add(0);

        for (int ch = 0; ch < deviceChannels; ch++)
        {
            double rms = Math.Sqrt((double)sumSq[ch] / (double)accumulator.Samples) / maxAmp;
            result.ChannelRms.Add(rms);

            double dbfs = rms <= 0 ? noiseFloor : 20.0 * Math.Log10(rms);
            result.ChannelDbfs.Add(dbfs);

            result.PeakAmplitude.Add(peakValues[ch] / maxAmp);
        }

        // Calculate SNR as difference between signal peak and noise floor
        if (result.PeakAmplitude.Any(p => p > 0.0001))
        {
            double peakDb = 20.0 * Math.Log10(result.PeakAmplitude.Max());
            result.SignalToNoiseRatio = peakDb - noiseFloor;
            result.HasSignal = true;
        }
        else
        {
            result.SignalToNoiseRatio = 0;
            result.HasSignal = false;
        }

        return result;
    }

    private MemoryStream GenerateTestToneWav(double frequency, int sampleRate, double durationSeconds)
    {
        int channels = 2;
        int totalSamples = (int)(sampleRate * durationSeconds);
        double amplitude = 0.5; // Use 50% amplitude to avoid clipping

        // Generate raw audio data
        var audioData = new List<short>();
        for (int i = 0; i < totalSamples; i++)
        {
            double t = i / (double)sampleRate;
            double phase = 2.0 * Math.PI * frequency * t;
            short sample = (short)(amplitude * short.MaxValue * Math.Sin(phase));
            
            // Duplicate for stereo
            audioData.Add(sample);
            audioData.Add(sample);
        }

        // Write WAV file header
        var ms = new MemoryStream();
        var bw = new BinaryWriter(ms);

        int byteRate = sampleRate * channels * 2; // 2 bytes per sample (16-bit)
        int subchunk2Size = totalSamples * channels * 2;

        // RIFF header
        bw.Write(new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
        bw.Write(36 + subchunk2Size); // File size - 8
        bw.Write(new byte[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });

        // fmt subchunk
        bw.Write(new byte[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
        bw.Write(16); // Subchunk1Size
        bw.Write((short)1); // AudioFormat (PCM)
        bw.Write((short)channels);
        bw.Write(sampleRate);
        bw.Write(byteRate);
        bw.Write((short)(channels * 2)); // BlockAlign
        bw.Write((short)16); // BitsPerSample

        // data subchunk
        bw.Write(new byte[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
        bw.Write(subchunk2Size);

        // Write audio data
        foreach (var sample in audioData)
        {
            bw.Write(sample);
        }

        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }
}

/// <summary>
/// Accumulates audio samples during recording
/// </summary>
public class AudioAccumulator
{
    private readonly ISoundDevice _device;
    public int Samples { get; private set; } = 0;
    public List<long> SumSq { get; private set; } = new();
    public List<double> PeakValues { get; private set; } = new();
    private readonly object _lock = new();

    public AudioAccumulator(ISoundDevice device)
    {
        _device = device;
        int channels = (int)(device?.Settings?.RecordingChannels ?? 2);
        for (int i = 0; i < channels; i++)
        {
            SumSq.Add(0);
            PeakValues.Add(0);
        }
    }

    public void OnData(byte[] buffer)
    {
        lock (_lock)
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
            while (PeakValues.Count < channels)
                PeakValues.Add(0);

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
                    
                    double absSample = Math.Abs((double)sample);
                    if (absSample > PeakValues[ch])
                        PeakValues[ch] = absSample;
                }
            }

            Samples += frameCount;
        }
    }
}
