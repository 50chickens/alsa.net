#nullable enable

using AlsaSharp;
using AlsaSharp.Library;
using AlsaSharp.Library.Logging;
using Example.SNRReduction.Services;
using Example.SNRReduction.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Example.SNRReduction.Audio;

public class AlsaLoopbackTestService(ILog<AlsaLoopbackTestService> log, IOptions<LoopbackTestOptions> options) : IAlsaLoopbackTestService
{
    private const double noiseFloor = -150.0;
    private readonly ILog<AlsaLoopbackTestService> _log = log;
    private readonly LoopbackTestOptions _options = options.Value;

    public (AlsaLoopbackTestResult PlayedSignal, AlsaLoopbackTestResult RecordedSignal, bool IsLoopbackWorking, LoopbackTestResult FullResult) TestLoopback(
        ISoundDevice playbackDevice,
        ISoundDevice recordingDevice,
        int testDurationMs = 3000,
        double testLevelDbfs = -12.0)
    {
        int sampleRate = (int)recordingDevice.Settings.RecordingSampleRate;
        int channels = recordingDevice.Settings.RecordingChannels;
        int bitsPerSample = recordingDevice.Settings.RecordingBitsPerSample;

        float pulseAmplitude = (float)Math.Pow(10.0, testLevelDbfs / 20.0);
        var monitor = new PulseLatencyMonitor(sampleRate, pulseAmplitude);
        
        using var cts = new CancellationTokenSource(testDurationMs + 2000);
        var (recordTask, playTask) = StartLoopbackTasks(recordingDevice, playbackDevice, monitor, 
            sampleRate, channels, bitsPerSample, cts.Token);

        Task.WaitAll(playTask, recordTask);

        var recordedResult = CreateRecordedResult(monitor, sampleRate, testDurationMs);
        bool isLoopbackWorking = monitor.TotalMeasurements > 0;

        var thd = CalculateTHD(monitor.PulseSamples, sampleRate);

        LogResults(recordedResult, monitor, isLoopbackWorking, thd);

        var fullResult = CreateFullResult(recordingDevice, monitor, sampleRate, testDurationMs, 
            testLevelDbfs, pulseAmplitude, isLoopbackWorking, thd);
        
        SaveResultToJson(fullResult);

        var playedResult = CreatePlaybackResult(sampleRate, testDurationMs);
        return (playedResult, recordedResult, isLoopbackWorking, fullResult);
    }

    private (Task RecordTask, Task PlayTask) StartLoopbackTasks(
        ISoundDevice recordingDevice,
        ISoundDevice playbackDevice,
        PulseLatencyMonitor monitor,
        int sampleRate,
        int channels,
        int bitsPerSample,
        CancellationToken cancellationToken)
    {
        int bytesPerFrame = channels * bitsPerSample / 8;
        var playbackReady = new ManualResetEventSlim(false);

        var recordTask = Task.Run(() => RunRecording(recordingDevice, monitor, 
            bytesPerFrame, channels, playbackReady, cancellationToken));

        var playTask = Task.Run(() => RunPlayback(playbackDevice, monitor, sampleRate, channels, 
            bitsPerSample, bytesPerFrame, playbackReady, cancellationToken));

        return (recordTask, playTask);
    }

    private void RunRecording(
        ISoundDevice device,
        PulseLatencyMonitor monitor,
        int bytesPerFrame,
        int channels,
        ManualResetEventSlim playbackReady,
        CancellationToken cancellationToken)
    {
        try
        {
            device.Record((frameData) =>
            {
                if (cancellationToken.IsCancellationRequested) return;
                
                int frameCount = frameData.Length / bytesPerFrame;
                for (int i = 0; i < frameCount; i++)
                {
                    short sample = BitConverter.ToInt16(frameData, i * bytesPerFrame);
                    monitor.ProcessInput(sample / 32768.0f);
                }

                playbackReady.Set();
            }, cancellationToken);
        }
        catch (OperationCanceledException) { }
    }

    private void RunPlayback(
        ISoundDevice device,
        PulseLatencyMonitor monitor,
        int sampleRate,
        int channels,
        int bitsPerSample,
        int bytesPerFrame,
        ManualResetEventSlim playbackReady,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!playbackReady.Wait(2000, cancellationToken)) return;

            device.PlayFromQueue(sampleRate, channels, bitsPerSample, (buffer) =>
            {
                if (cancellationToken.IsCancellationRequested) return 0;

                int frameCount = buffer.Length / bytesPerFrame;
                for (int i = 0; i < frameCount; i++)
                {
                    float sample = monitor.GenerateSample();
                    short s = (short)(sample * 32767.0f);
                    
                    for (int ch = 0; ch < channels; ch++)
                    {
                        int offset = (i * channels + ch) * (bitsPerSample / 8);
                        byte[] bytes = BitConverter.GetBytes(s);
                        Array.Copy(bytes, 0, buffer, offset, bytes.Length);
                    }
                }
                return buffer.Length;
            }, 10, cancellationToken);
        }
        catch (OperationCanceledException) { }
    }

    private AlsaLoopbackTestResult CreateRecordedResult(
        PulseLatencyMonitor monitor,
        int sampleRate,
        int testDurationMs)
    {
        double peakAmplitude = monitor.PeakAmplitude;
        double dbfs = peakAmplitude > 0 ? 20.0 * Math.Log10(peakAmplitude) : noiseFloor;
        
        var result = new AlsaLoopbackTestResult
        {
            Samples = (int)(sampleRate * (testDurationMs / 1000.0)),
            ChannelDbfs = new() { dbfs, dbfs },
            ChannelRms = new() { peakAmplitude * 0.707, peakAmplitude * 0.707 },
            PeakAmplitude = new() { peakAmplitude, peakAmplitude },
            SignalToNoiseRatio = dbfs - noiseFloor,
            TotalHarmonicDistortionDb = double.NaN,
            HasSignal = peakAmplitude > 0.0001
        };
        
        if (monitor.TotalMeasurements > 0)
        {
            result.RoundTripLatencyMs = monitor.AverageLatencyMs;
            result.LatencySamples = (int)monitor.AverageLatencySamples;
            result.ConfidenceScore = 1.0;
            result.LatencyMeasurementValid = true;
        }
        
        return result;
    }

    private void LogResults(AlsaLoopbackTestResult result, PulseLatencyMonitor monitor, bool isWorking, THDResult thd)
    {
        if (isWorking)
        {
            _log.Info($"Latency: {monitor.AverageLatencyMs:F2}ms (min: {monitor.MinLatencyMs:F2}ms, max: {monitor.MaxLatencyMs:F2}ms)");
            _log.Info($"Measurements: {monitor.TotalMeasurements}, Signal: {result.ChannelDbfs[0]:F1}dBFS");
            if (thd.IsValid)
            {
                _log.Info($"THD: {thd.ThdPercent:F4}% ({thd.ThdDb:F2}dB), Fundamental: {thd.FundamentalFrequency:F1}Hz");
                if (thd.Harmonics != null && thd.Harmonics.Length > 0)
                {
                    _log.Info($"  H2: {thd.Harmonics[0].MagnitudeDb:F2}dB, H3: {thd.Harmonics[1].MagnitudeDb:F2}dB, H4: {thd.Harmonics[2].MagnitudeDb:F2}dB, H5: {thd.Harmonics[3].MagnitudeDb:F2}dB");
                }
            }
            else
            {
                _log.Info("THD: Not available (insufficient signal or duration)");
            }
        }
        else
        {
            _log.Warn("No pulse detected - check loopback cable");
        }
    }

    private AlsaLoopbackTestResult CreatePlaybackResult(int sampleRate, int testDurationMs)
    {
        return new AlsaLoopbackTestResult
        {
            Samples = (int)(sampleRate * (testDurationMs / 1000.0)),
            ChannelDbfs = new() { -12.0, -12.0 },
            ChannelRms = new() { 0.25, 0.25 },
            PeakAmplitude = new() { 0.25, 0.25 },
            SignalToNoiseRatio = 147.0,
            TotalHarmonicDistortionDb = double.NaN,
            HasSignal = true
        };
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

    private class PulseLatencyMonitor
    {
        private enum State { Idle, Pulsing, Measuring, CaptureWindow }

        private const float IDLE_AMPLITUDE = 0.001f;
        private const int PULSE_DURATION = 1920; // 40ms at 48kHz = 40 cycles of 1kHz for reliable THD
        private const int MIN_LATENCY_SAMPLES = 100;
        private const int CAPTURE_WINDOW = 2400; // 50ms window to capture pulse

        private readonly int _sampleRate;
        private readonly float _pulseAmplitude;
        private readonly float _detectThreshold;
        private readonly int _idleDuration;
        private readonly int _maxLatencySamples;
        private readonly List<float> _rawSamples = new();
        private readonly List<float> _pulseSamples = new();
        private readonly float[] _circularBuffer = new float[CAPTURE_WINDOW];
        private int _circularBufferIndex = 0;
        private int _pulseSampleIndex = 0;
        
        private State _state = State.Idle;
        private int _counter;
        private readonly List<int> _measurements = new();
        private long _sumLatency;
        private int _minLatency = int.MaxValue;
        private int _maxLatency = 0;
        private float _peakAmplitude = 0.0f;

        public int TotalMeasurements => _measurements.Count;
        public double PeakAmplitude => _peakAmplitude;
        public IReadOnlyList<float> RawSamples => _rawSamples;
        public IReadOnlyList<float> PulseSamples => _pulseSamples;
        public double AverageLatencyMs => TotalMeasurements > 0 ? (_sumLatency * 1000.0 / TotalMeasurements / _sampleRate) : 0;
        public double MinLatencyMs => _minLatency < int.MaxValue ? (_minLatency * 1000.0 / _sampleRate) : 0;
        public double MaxLatencyMs => _maxLatency > 0 ? (_maxLatency * 1000.0 / _sampleRate) : 0;
        public double AverageLatencySamples => TotalMeasurements > 0 ? (_sumLatency / (double)TotalMeasurements) : 0;

        public PulseLatencyMonitor(int sampleRate, float pulseAmplitude = 0.25f, double idleSeconds = 0.5, int maxLatencyMs = 100)
        {
            _sampleRate = sampleRate;
            _pulseAmplitude = pulseAmplitude;
            _detectThreshold = pulseAmplitude * 0.4f;
            _idleDuration = (int)(idleSeconds * sampleRate);
            _maxLatencySamples = maxLatencyMs * sampleRate / 1000;
            _counter = _idleDuration;
        }

        public float GenerateSample()
        {
            switch (_state)
            {
                case State.Idle:
                    if (--_counter <= 0)
                    {
                        _state = State.Pulsing;
                        _counter = PULSE_DURATION;
                        _pulseSampleIndex = 0;
                    }
                    return IDLE_AMPLITUDE;

                case State.Pulsing:
                    if (--_counter <= 0)
                    {
                        _state = State.Measuring;
                        _counter = 0;
                        return 0.0f;
                    }
                    // Generate 1kHz sine wave for THD analysis
                    double frequency = 1000.0;
                    float sample = _pulseAmplitude * (float)Math.Sin(2 * Math.PI * frequency * _pulseSampleIndex / _sampleRate);
                    _pulseSampleIndex++;
                    return sample;

                case State.Measuring:
                    return 0.0f;

                default:
                    return IDLE_AMPLITUDE;
            }
        }

        public void ProcessInput(float sample)
        {
            if (_rawSamples.Count < _sampleRate * 5)
                _rawSamples.Add(sample);
            
            // Always store in circular buffer for retrospective capture
            _circularBuffer[_circularBufferIndex] = sample;
            _circularBufferIndex = (_circularBufferIndex + 1) % CAPTURE_WINDOW;
            
            if (_state == State.Measuring)
            {
                _counter++;
                
                float absSample = Math.Abs(sample);
                
                if (absSample > _detectThreshold && _counter >= MIN_LATENCY_SAMPLES)
                {
                    _measurements.Add(_counter);
                    _sumLatency += _counter;
                    _minLatency = Math.Min(_minLatency, _counter);
                    _maxLatency = Math.Max(_maxLatency, _counter);
                    
                    if (absSample > _peakAmplitude)
                        _peakAmplitude = absSample;
                    
                    // Capture the pulse from circular buffer
                    CaptureCircularBuffer();
                    
                    // Enter capture window state to get remaining samples
                    _state = State.CaptureWindow;
                    _counter = PULSE_DURATION; // Capture remaining pulse duration
                }
                else if (_counter >= _maxLatencySamples)
                {
                    _state = State.Idle;
                    _counter = _idleDuration;
                }
            }
            else if (_state == State.CaptureWindow)
            {
                _pulseSamples.Add(sample);
                _counter--;
                
                if (_counter <= 0)
                {
                    _state = State.Idle;
                    _counter = _idleDuration;
                }
            }
        }
        
        private void CaptureCircularBuffer()
        {
            // Capture samples from circular buffer (retrospective capture)
            for (int i = 0; i < CAPTURE_WINDOW; i++)
            {
                int index = (_circularBufferIndex + i) % CAPTURE_WINDOW;
                _pulseSamples.Add(_circularBuffer[index]);
            }
        }
    }

    private THDResult CalculateTHD(IReadOnlyList<float> samples, int sampleRate)
    {
        _log.Debug($"[THD] Analyzing {samples.Count} samples at {sampleRate}Hz sample rate");
        
        // Need at least 1000 samples (about 21ms at 48kHz) for reasonable THD
        if (samples.Count < 1000)
        {
            _log.Debug($"[THD] Insufficient samples: {samples.Count} < 1000");
            return new THDResult { IsValid = false };
        }

        const double testFrequency = 1000.0;
        var fundamental = GoertzelMagnitude(samples, sampleRate, testFrequency);
        
        _log.Debug($"[THD] Fundamental at {testFrequency}Hz: magnitude = {fundamental:F6}");
        
        if (fundamental < 0.001)
        {
            _log.Debug($"[THD] Fundamental too weak: {fundamental:F6} < 0.001");
            return new THDResult { IsValid = false };
        }

        var h2 = GoertzelMagnitude(samples, sampleRate, testFrequency * 2);
        var h3 = GoertzelMagnitude(samples, sampleRate, testFrequency * 3);
        var h4 = GoertzelMagnitude(samples, sampleRate, testFrequency * 4);
        var h5 = GoertzelMagnitude(samples, sampleRate, testFrequency * 5);

        _log.Debug($"[THD] Harmonics: H2={h2:F6}, H3={h3:F6}, H4={h4:F6}, H5={h5:F6}");

        double sumHarmonics = Math.Sqrt(h2 * h2 + h3 * h3 + h4 * h4 + h5 * h5);
        double thdPercent = (sumHarmonics / fundamental) * 100.0;
        double thdDb = 20 * Math.Log10(sumHarmonics / fundamental);

        _log.Debug($"[THD] THD: {thdPercent:F3}% ({thdDb:F2}dB)");

        return new THDResult
        {
            IsValid = true,
            ThdPercent = thdPercent,
            ThdDb = thdDb,
            FundamentalFrequency = testFrequency,
            FundamentalMagnitude = fundamental,
            Harmonics = new[]
            {
                new HarmonicData { HarmonicNumber = 2, Frequency = testFrequency * 2, MagnitudeLinear = h2, MagnitudeDb = 20 * Math.Log10(h2 / fundamental) },
                new HarmonicData { HarmonicNumber = 3, Frequency = testFrequency * 3, MagnitudeLinear = h3, MagnitudeDb = 20 * Math.Log10(h3 / fundamental) },
                new HarmonicData { HarmonicNumber = 4, Frequency = testFrequency * 4, MagnitudeLinear = h4, MagnitudeDb = 20 * Math.Log10(h4 / fundamental) },
                new HarmonicData { HarmonicNumber = 5, Frequency = testFrequency * 5, MagnitudeLinear = h5, MagnitudeDb = 20 * Math.Log10(h5 / fundamental) }
            }
        };
    }

    private double GoertzelMagnitude(IReadOnlyList<float> samples, int sampleRate, double targetFrequency)
    {
        int N = Math.Min(samples.Count, sampleRate);
        double k = 0.5 + (N * targetFrequency / sampleRate);
        double omega = (2.0 * Math.PI * k) / N;
        double coeff = 2.0 * Math.Cos(omega);
        
        double q0 = 0.0, q1 = 0.0, q2 = 0.0;
        
        for (int i = 0; i < N; i++)
        {
            q0 = coeff * q1 - q2 + samples[i];
            q2 = q1;
            q1 = q0;
        }
        
        double real = q1 - q2 * Math.Cos(omega);
        double imag = q2 * Math.Sin(omega);
        double magnitude = Math.Sqrt(real * real + imag * imag) / N * 2.0;
        
        return magnitude;
    }

    private LoopbackTestResult CreateFullResult(
        ISoundDevice device,
        PulseLatencyMonitor monitor,
        int sampleRate,
        int testDurationMs,
        double testLevelDbfs,
        float pulseAmplitude,
        bool isLoopbackWorking,
        THDResult thd)
    {
        double peakDbfs = monitor.PeakAmplitude > 0 ? 20 * Math.Log10(monitor.PeakAmplitude) : noiseFloor;

        return new LoopbackTestResult
        {
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            Metadata = new TestMetadata
            {
                AudioInterface = device.Settings.CardName ?? "Unknown",
                CardName = device.Settings.CardName ?? "Unknown",
                CardNumber = device.Settings.CardIndex ?? -1,
                PlaybackDevice = device.Settings.PlaybackDeviceName ?? "Unknown",
                RecordingDevice = device.Settings.RecordingDeviceName ?? "Unknown",
                Driver = "ALSA",
                SampleRate = sampleRate,
                Channels = device.Settings.RecordingChannels,
                BitsPerSample = device.Settings.RecordingBitsPerSample
            },
            Configuration = new TestConfiguration
            {
                TestDurationMs = testDurationMs,
                TestLevelDbfs = testLevelDbfs,
                PulseAmplitude = pulseAmplitude,
                PulseDurationSamples = 1920, // 40ms at 48kHz for reliable THD
                DetectThreshold = pulseAmplitude * 0.4
            },
            Measurements = new TestMeasurements
            {
                IsLoopbackWorking = isLoopbackWorking,
                TotalMeasurements = monitor.TotalMeasurements,
                AverageLatencyMs = monitor.AverageLatencyMs,
                MinLatencyMs = monitor.MinLatencyMs,
                MaxLatencyMs = monitor.MaxLatencyMs,
                AverageLatencySamples = monitor.AverageLatencySamples,
                PeakAmplitudeDbfs = peakDbfs,
                PeakAmplitudeLinear = monitor.PeakAmplitude,
                ThdPercent = thd.ThdPercent,
                ThdDb = thd.ThdDb,
                FundamentalFrequency = thd.FundamentalFrequency,
                Harmonics = thd.Harmonics
            }
        };
    }

    private void SaveResultToJson(LoopbackTestResult result)
    {
        try
        {
            var resultsFolder = _options.ResultsFolder;
            
            // Create results directory if it doesn't exist
            if (!Directory.Exists(resultsFolder))
            {
                Directory.CreateDirectory(resultsFolder);
                _log.Debug($"Created results directory: {resultsFolder}");
            }
            
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var filename = $"loopback_test_{timestamp}.json";
            var filepath = Path.Combine(resultsFolder, filename);

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(result, jsonOptions);
            File.WriteAllText(filepath, json);

            _log.Info($"Results saved to: {filepath}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to save JSON result");
        }
    }

    private class THDResult
    {
        public bool IsValid { get; set; }
        public double ThdPercent { get; set; }
        public double ThdDb { get; set; }
        public double FundamentalFrequency { get; set; }
        public double FundamentalMagnitude { get; set; }
        public HarmonicData[] Harmonics { get; set; } = Array.Empty<HarmonicData>();
    }
}
