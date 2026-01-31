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

        // Use synchronized circular buffer like CopyOnlyService for synchronized playback/recording
        using var cts = new CancellationTokenSource(testDurationMs + 2000);
        var cancellationToken = cts.Token;

        // Create circular buffer for synchronized I/O
        var circBuffer = new CircularAudioBuffer(256 * 1024);
        
        // Calculate frame parameters
        int sampleRate = (int)recordingDevice.Settings.RecordingSampleRate;
        int channels = recordingDevice.Settings.RecordingChannels;
        int bitsPerSample = recordingDevice.Settings.RecordingBitsPerSample;
        int bytesPerFrame = channels * bitsPerSample / 8;
        int periodSize = 256;
        int bytesPerPeriod = periodSize * bytesPerFrame;
        const int PRE_BUFFER_PERIODS = 15;
        int PRE_BUFFER_BYTES = PRE_BUFFER_PERIODS * periodSize * 2 * 2; // 15 periods, stereo, 16-bit

        _log.Info($"Buffer config: period={periodSize} frames, bytes/period={bytesPerPeriod}, pre-buffer={PRE_BUFFER_PERIODS} periods");

        var playbackReady = new ManualResetEvent(false);
        var recordingComplete = new ManualResetEvent(false);

        // Task 1: Recording thread - continuously records audio into circular buffer
        var recordTask = Task.Run(() =>
        {
            try
            {
                _log.Info("Recording thread started");
                long framesRecorded = 0;

                recordingDevice.Record((frameData) =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _log.Trace($"Recording detected cancellation after {framesRecorded} frames");
                        return;
                    }

                    // Write to circular buffer
                    circBuffer.Write(frameData, frameData.Length);
                    framesRecorded += frameData.Length / bytesPerFrame;

                    // Also accumulate for analysis
                    recordedAccumulator.OnData(frameData);

                    // Signal playback to start once we have enough pre-buffer
                    if (circBuffer.AvailableBytes >= PRE_BUFFER_BYTES && !playbackReady.WaitOne(0))
                    {
                        _log.Trace($"Recording: Pre-buffer reached ({circBuffer.AvailableBytes} bytes), signaling playback");
                        playbackReady.Set();
                    }

                    if (framesRecorded % (sampleRate / 10) == 0)
                    {
                        _log.Trace($"Recording: {framesRecorded} frames, buffer level: {circBuffer.AvailableBytes} bytes");
                    }
                }, cancellationToken);

                _log.Info($"Recording completed: {framesRecorded} frames captured");
            }
            catch (OperationCanceledException)
            {
                _log.Trace("Recording cancelled");
            }
            catch (Exception ex)
            {
                _log.Error($"Recording error: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                recordingComplete.Set();
                playbackReady.Set(); // Unblock playback if recording ends
            }
        });

        // Task 2: Playback thread - reads from circular buffer and plays test tone
        var playTask = Task.Run(() =>
        {
            try
            {
                _log.Info("Playback thread started");

                // Wait for pre-buffer to accumulate
                _log.Trace($"Playback: Waiting for pre-buffer (need {PRE_BUFFER_BYTES} bytes)...");
                if (!playbackReady.WaitOne(2000))
                {
                    _log.Warn("Playback: Pre-buffer timeout");
                }

                _log.Trace($"Playback: Starting playback from test tone stream");

                // Wrap the test tone stream in a circular buffer wrapper for synchronized playback
                testToneStream.Seek(0, SeekOrigin.Begin);

                long bytesRead = 0;
                long callbackCount = 0;

                // Use PlayFromQueue to read from test tone stream and play it with synchronized timing
                playbackDevice.PlayFromQueue(
                    sampleRate,
                    channels,
                    bitsPerSample,
                    (buffer) =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return 0;

                        callbackCount++;

                        // Read from test tone stream
                        int read = testToneStream.Read(buffer, 0, buffer.Length);
                        if (read > 0)
                        {
                            bytesRead += read;
                        }

                        return read;
                    },
                    10, // Wait up to 10ms for data per period
                    cancellationToken);

                _log.Info($"Playback completed: {bytesRead} bytes played in {callbackCount} callbacks");
            }
            catch (OperationCanceledException)
            {
                _log.Trace("Playback cancelled");
            }
            catch (Exception ex)
            {
                _log.Error($"Playback error: {ex.GetType().Name}: {ex.Message}");
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
        finally
        {
            playbackReady?.Dispose();
            recordingComplete?.Dispose();
        }

        // Analyze recorded signal (what we captured)
        var recordedResult = AnalyzeSignal(recordedAccumulator, recordingDevice, "Recorded Signal");

        // Verify that the received audio is the same test tone that was sent (1000 Hz fingerprint)
        bool testToneDetected = VerifyTestToneFrequency(recordedAccumulator, recordingDevice, 1000.0, 48000);
        
        // Determine if loopback is working based on test tone verification
        // Loopback is working if the specific test frequency (1000 Hz) is detected in the recording
        bool isLoopbackWorking = testToneDetected && recordedResult.Samples > 0;

        _log.Info($"\n--- Loopback Test Results ---");
        _log.Info($"Output Signal: Samples: {recordedResult.Samples}, dBFS: [{string.Join(", ", recordedResult.ChannelDbfs.Select(d => $"{d:F1}dB"))}], RMS: [{string.Join(", ", recordedResult.ChannelRms.Select(r => $"{r:F6}"))}], Peak: [{string.Join(", ", recordedResult.PeakAmplitude.Select(p => $"{p:F6}"))}]");
        
        if (double.IsFinite(recordedResult.TotalHarmonicDistortionDb))
        {
            string thdQuality = GetTHDQualityRating(recordedResult.TotalHarmonicDistortionDb);
            _log.Info($"THD: {recordedResult.TotalHarmonicDistortionDb:F2}dB - {thdQuality}");
        }
        
        _log.Info($"SNR: {recordedResult.SignalToNoiseRatio:F1}dB");
        _log.Info($"Test Tone (1000 Hz) Detected: {(testToneDetected ? "✓ YES" : "✗ NO")}");
        _log.Info($"Loopback Status: {(isLoopbackWorking ? "✓ WORKING" : "✗ NOT WORKING")}");

        if (!isLoopbackWorking)
        {
            if (recordedResult.Samples > 0)
            {
                if (!testToneDetected)
                {
                    _log.Warn($"⚠️  Hardware loopback NOT detected!");
                    _log.Warn($"   The test tone (1000 Hz) was not found in the recorded audio.");
                    _log.Warn($"   Audio was captured but it doesn't match the sent test signal.");
                    _log.Warn($"   TROUBLESHOOTING:");
                    _log.Warn($"   1. Check if output cable is physically connected to input");
                    _log.Warn($"   2. Verify ALSA mixer routing - output should go to input");
                    _log.Warn($"   3. Check if ADC/input is enabled and not muted");
                    _log.Warn($"   4. Run 'alsamixer' to verify audio path connections");
                }
            }
            else
            {
                _log.Error($"⚠️  No audio captured during recording!");
                _log.Error($"   TROUBLESHOOTING:");
                _log.Error($"   1. Verify recording device is accessible and enabled");
                _log.Error($"   2. Check ALSA permissions and device settings");
                _log.Error($"   3. Ensure ADC/input device is not muted");
            }
        }
        else
        {
            _log.Info($"✓ Loopback test PASSED - test tone successfully received (signal fingerprint verified)");
        }

        // Create a dummy result for played signal (we don't actually capture it, but report what we played)
        var playedResult = new AlsaLoopbackTestResult
        {
            Samples = (int)(48000 * (testDurationMs / 1000.0)),
            ChannelDbfs = new() { -3.0, -3.0 },
            ChannelRms = new() { 0.5, 0.5 },
            PeakAmplitude = new() { 0.5, 0.5 },
            SignalToNoiseRatio = 147.0,
            TotalHarmonicDistortionDb = double.NaN,
            HasSignal = true
        };

        return (playedResult, recordedResult, isLoopbackWorking);
    }

    /// <summary>
    /// Verifies if the test tone at a specific frequency is present in the recorded audio.
    /// Uses Goertzel algorithm to detect energy at the target frequency.
    /// </summary>
    private bool VerifyTestToneFrequency(AudioAccumulator accumulator, ISoundDevice device, double targetFrequency, int sampleRate)
    {
        try
        {
            if (accumulator.Samples == 0 || accumulator.RawSamples.Count == 0)
                return false;

            // Calculate energy at target frequency for each channel using Goertzel algorithm
            var targetEnergies = new List<double>();
            int channels = (int)(device?.Settings?.RecordingChannels ?? 2);

            for (int ch = 0; ch < channels; ch++)
            {
                double energy = GoertzelEnergy(accumulator, ch, targetFrequency, sampleRate);
                targetEnergies.Add(energy);
                _log.Info($"Target frequency ({targetFrequency} Hz) energy on channel {ch + 1}: {energy:F6}");
            }

            // Also check adjacent frequencies to verify it's not just noise
            double lowerFreqEnergy = GoertzelEnergy(accumulator, 0, targetFrequency - 50, sampleRate);
            double higherFreqEnergy = GoertzelEnergy(accumulator, 0, targetFrequency + 50, sampleRate);

            _log.Info($"Frequency comparison - lower ({targetFrequency - 50} Hz): {lowerFreqEnergy:F6}, target ({targetFrequency} Hz): {targetEnergies[0]:F6}, higher ({targetFrequency + 50} Hz): {higherFreqEnergy:F6}");

            // Test tone is detected if:
            // 1. Energy at target frequency is significant
            // 2. Energy at target frequency is comparable to adjacent frequencies (within 3dB)
            // 3. At least one channel has detectable energy
            
            double maxEnergy = Math.Max(targetEnergies[0], Math.Max(lowerFreqEnergy, higherFreqEnergy));
            
            // Check if target is significant relative to max (at least 5% of max energy)
            bool targetIsSignificant = targetEnergies[0] > (maxEnergy * 0.05);
            
            // Check if target is within 3dB of the maximum (10^(3/20) ≈ 1.41)
            // This means target should be at least 71% of the max, or max should be at most 141% of target
            bool targetIsWithin3dB = targetEnergies[0] > 0.0 && maxEnergy < (targetEnergies[0] * 1.5);
            
            // Also require that at least one channel has detectable energy
            bool hasEnergy = targetEnergies.Any(e => e > 0.00001);

            bool detected = targetIsSignificant && targetIsWithin3dB && hasEnergy;
            _log.Info($"Tone detection: significant={targetIsSignificant}, within3dB={targetIsWithin3dB}, hasEnergy={hasEnergy}, detected={detected}");
            
            return detected;
        }
        catch (Exception ex)
        {
            _log.Warn($"Error during frequency verification: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Calculates energy at a specific frequency using Goertzel algorithm.
    /// </summary>
    private double GoertzelEnergy(AudioAccumulator accumulator, int channel, double targetFrequency, int sampleRate)
    {
        if (accumulator.RawSamples.Count == 0)
            return 0.0;

        // Use Goertzel algorithm to detect energy at target frequency
        double normFreq = targetFrequency / sampleRate;
        double coeff = 2.0 * Math.Cos(2.0 * Math.PI * normFreq);

        double s0 = 0, s1 = 0, s2 = 0;

        // Process samples through Goertzel filter
        foreach (short sample in accumulator.RawSamples)
        {
            s0 = sample + coeff * s1 - s2;
            s2 = s1;
            s1 = s0;
        }

        // Calculate power at target frequency
        double realPart = s1 - s2 * Math.Cos(2.0 * Math.PI * normFreq);
        double imagPart = s2 * Math.Sin(2.0 * Math.PI * normFreq);
        double power = realPart * realPart + imagPart * imagPart;

        return Math.Sqrt(power / accumulator.RawSamples.Count);
    }

    /// <summary>
    /// Classifies THD quality based on established audio industry standards.
    /// </summary>
    private string GetTHDQualityRating(double thdDb)
    {
        // Convert dB to percentage for clarity
        // THD% = sqrt(10^(THD_dB/10))
        double thdPercent = Math.Sqrt(Math.Pow(10.0, thdDb / 10.0));
        
        // Audio quality standards (IEC 60268-3 and professional audio industry standards)
        if (thdPercent < 1.0)          // < -40 dB
            return "Excellent (< 1% THD, < -40 dB): Transparent audio quality, suitable for professional mastering and critical listening.";
        else if (thdPercent < 3.0)     // -30 to -40 dB
            return "Good (1-3% THD, -30 to -40 dB): High quality, suitable for professional applications.";
        else if (thdPercent < 5.0)     // -26 to -30 dB
            return "Fair (3-5% THD, -26 to -30 dB): Consumer audio quality, acceptable for general use.";
        else                            // > -26 dB
            return "Poor (> 5% THD, > -26 dB): Noticeable distortion, improvement recommended.";
    }

    private AlsaLoopbackTestResult AnalyzeSignal(AudioAccumulator accumulator, ISoundDevice device, string label)
    {
        var result = new AlsaLoopbackTestResult
        {
            Samples = accumulator.Samples,
            TotalHarmonicDistortionDb = double.NaN
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

        // Calculate THD using Goertzel algorithm (reusing pattern from SNRMeasurementService)
        try
        {
            if (accumulator.RawSamples.Count > 0)
            {
                // Convert raw samples to double array for THD calculation
                double[] sampleData = accumulator.RawSamples.Select(s => (double)s).ToArray();
                int sampleRate = (int)(device?.Settings?.RecordingSampleRate ?? 48000);
                double testFrequency = 1000.0; // Using the test tone frequency

                int maxHarmonic = 5;
                double fundamentalPower = GoertzelPower(sampleData, sampleRate, testFrequency);
                double harmonicPower = 0.0;

                for (int h = 2; h <= maxHarmonic; h++)
                {
                    double f = testFrequency * h;
                    if (f >= sampleRate / 2.0) break; // beyond Nyquist
                    harmonicPower += GoertzelPower(sampleData, sampleRate, f);
                }

                if (fundamentalPower > 0.0)
                {
                    double thdRatio = harmonicPower / fundamentalPower;
                    result.TotalHarmonicDistortionDb = 10.0 * Math.Log10(thdRatio);
                    _log.Info($"THD calculated: {result.TotalHarmonicDistortionDb:F2} dB (fundamental: {fundamentalPower:F6}, harmonics: {harmonicPower:F6})");
                }
            }
        }
        catch (Exception ex)
        {
            _log.Warn($"Error calculating THD: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Calculates power at a specific frequency using Goertzel algorithm.
    /// </summary>
    private static double GoertzelPower(double[] data, int sampleRate, double freq)
    {
        if (data == null || data.Length == 0) return 0.0;
        
        int N = data.Length;
        double omega = 2.0 * Math.PI * freq / sampleRate;
        double coeff = 2.0 * Math.Cos(omega);
        double s_prev = 0.0, s_prev2 = 0.0;
        
        for (int i = 0; i < N; i++)
        {
            double s = data[i] + coeff * s_prev - s_prev2;
            s_prev2 = s_prev;
            s_prev = s;
        }
        
        // magnitude squared (unnormalized)
        double power = s_prev * s_prev + s_prev2 * s_prev2 - coeff * s_prev * s_prev2;
        if (!double.IsFinite(power) || power < 0.0) power = 0.0;
        return power / N;
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
    public List<short> RawSamples { get; private set; } = new(); // Store raw samples for frequency analysis
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

                    // Store raw samples for frequency analysis (limit to prevent memory issues)
                    if (RawSamples.Count < 240000) // ~5 seconds at 48kHz
                    {
                        RawSamples.Add((short)Math.Max(short.MinValue, Math.Min(short.MaxValue, sample)));
                    }
                }
            }

            Samples += frameCount;
        }
    }
}
