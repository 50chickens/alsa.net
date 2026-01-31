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
        int sampleRate = (int)recordingDevice.Settings.RecordingSampleRate;
        int channels = recordingDevice.Settings.RecordingChannels;
        int bitsPerSample = recordingDevice.Settings.RecordingBitsPerSample;

        var monitor = new PulseLatencyMonitor(sampleRate);
        
        using var cts = new CancellationTokenSource(testDurationMs + 2000);
        var (recordTask, playTask) = StartLoopbackTasks(recordingDevice, playbackDevice, monitor, 
            sampleRate, channels, bitsPerSample, cts.Token);

        Task.WaitAll(playTask, recordTask);

        var recordedResult = CreateRecordedResult(monitor, sampleRate, testDurationMs);
        bool isLoopbackWorking = monitor.TotalMeasurements > 0;

        LogResults(recordedResult, monitor, isLoopbackWorking);

        var playedResult = CreatePlaybackResult(sampleRate, testDurationMs);
        return (playedResult, recordedResult, isLoopbackWorking);
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

    private void LogResults(AlsaLoopbackTestResult result, PulseLatencyMonitor monitor, bool isWorking)
    {
        if (isWorking)
        {
            _log.Info($"Latency: {monitor.AverageLatencyMs:F2}ms (min: {monitor.MinLatencyMs:F2}ms, max: {monitor.MaxLatencyMs:F2}ms)");
            _log.Info($"Measurements: {monitor.TotalMeasurements}, Signal: {result.ChannelDbfs[0]:F1}dBFS");
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
        private enum State { Idle, Pulsing, Measuring }

        private const float IDLE_AMPLITUDE = 0.001f;
        private const float PULSE_AMPLITUDE = 0.25f;
        private const int PULSE_DURATION = 100;
        private const float DETECT_THRESHOLD = 0.1f;
        private const int MIN_LATENCY_SAMPLES = 100;

        private readonly int _sampleRate;
        private readonly int _idleDuration;
        private readonly int _maxLatencySamples;
        
        private State _state = State.Idle;
        private int _counter;
        private readonly List<int> _measurements = new();
        private long _sumLatency;
        private int _minLatency = int.MaxValue;
        private int _maxLatency = 0;
        private float _peakAmplitude = 0.0f;

        public int TotalMeasurements => _measurements.Count;
        public double PeakAmplitude => _peakAmplitude;
        public double AverageLatencyMs => TotalMeasurements > 0 ? (_sumLatency * 1000.0 / TotalMeasurements / _sampleRate) : 0;
        public double MinLatencyMs => _minLatency < int.MaxValue ? (_minLatency * 1000.0 / _sampleRate) : 0;
        public double MaxLatencyMs => _maxLatency > 0 ? (_maxLatency * 1000.0 / _sampleRate) : 0;
        public double AverageLatencySamples => TotalMeasurements > 0 ? (_sumLatency / (double)TotalMeasurements) : 0;

        public PulseLatencyMonitor(int sampleRate, double idleSeconds = 0.5, int maxLatencyMs = 100)
        {
            _sampleRate = sampleRate;
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
                    }
                    return IDLE_AMPLITUDE;

                case State.Pulsing:
                    if (--_counter <= 0)
                    {
                        _state = State.Measuring;
                        _counter = 0;
                        return 0.0f;
                    }
                    return PULSE_AMPLITUDE;

                case State.Measuring:
                    return 0.0f;

                default:
                    return IDLE_AMPLITUDE;
            }
        }

        public void ProcessInput(float sample)
        {
            if (_state == State.Measuring)
            {
                _counter++;
                
                float absSample = Math.Abs(sample);
                
                if (absSample > DETECT_THRESHOLD && _counter >= MIN_LATENCY_SAMPLES)
                {
                    _measurements.Add(_counter);
                    _sumLatency += _counter;
                    _minLatency = Math.Min(_minLatency, _counter);
                    _maxLatency = Math.Max(_maxLatency, _counter);
                    
                    if (absSample > _peakAmplitude)
                        _peakAmplitude = absSample;
                    
                    _state = State.Idle;
                    _counter = _idleDuration;
                }
                else if (_counter >= _maxLatencySamples)
                {
                    _state = State.Idle;
                    _counter = _idleDuration;
                }
            }
        }
    }
}
