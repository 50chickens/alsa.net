using AlsaSharp;

namespace AlsaSharp.Library.Operations.Services;

public interface IAlsaLoopbackTestService
{
    /// <summary>
    /// Tests ALSA loopback by simultaneously playing a test tone and recording it.
    /// Returns tuple of (PlayedSignalStats, RecordedSignalStats, IsLoopbackWorking, FullResult)
    /// </summary>
    (AlsaLoopbackTestResult PlayedSignal, AlsaLoopbackTestResult RecordedSignal, bool IsLoopbackWorking, LoopbackTestResult FullResult) TestLoopback(
        ISoundDevice playbackDevice, 
        ISoundDevice recordingDevice, 
        int testDurationMs = 3000,
        double testLevelDbfs = -12.0);
}

public class AlsaLoopbackTestResult
{
    public int Samples { get; set; }
    public List<double> ChannelDbfs { get; set; } = new();
    public List<double> ChannelRms { get; set; } = new();
    public List<double> PeakAmplitude { get; set; } = new();
    public double SignalToNoiseRatio { get; set; }
    public double TotalHarmonicDistortionDb { get; set; }
    public bool HasSignal { get; set; }
    public double RoundTripLatencyMs { get; set; } = -1.0;
    public int LatencySamples { get; set; } = -1;
    public double ConfidenceScore { get; set; } = 0.0;
    public bool LatencyMeasurementValid { get; set; } = false;
    public double OutputLatencyMs { get; set; } = -1.0;
    public double InputLatencyMs { get; set; } = -1.0;
}

public class LoopbackTestResult
{
    public string Timestamp { get; set; } = string.Empty;
    public TestMetadata Metadata { get; set; } = new();
    public TestConfiguration Configuration { get; set; } = new();
    public TestMeasurements Measurements { get; set; } = new();
}

public class TestMetadata
{
    public string AudioInterface { get; set; } = string.Empty;
    public string CardName { get; set; } = string.Empty;
    public int CardNumber { get; set; }
    public string PlaybackDevice { get; set; } = string.Empty;
    public string RecordingDevice { get; set; } = string.Empty;
    public string Driver { get; set; } = string.Empty;
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public int BitsPerSample { get; set; }
}

public class TestConfiguration
{
    public int TestDurationMs { get; set; }
    public double TestLevelDbfs { get; set; }
    public double PulseAmplitude { get; set; }
    public int PulseDurationSamples { get; set; }
    public double DetectThreshold { get; set; }
}

public class TestMeasurements
{
    public bool IsLoopbackWorking { get; set; }
    public int TotalMeasurements { get; set; }
    public double AverageLatencyMs { get; set; }
    public double MinLatencyMs { get; set; }
    public double MaxLatencyMs { get; set; }
    public double AverageLatencySamples { get; set; }
    public double PeakAmplitudeDbfs { get; set; }
    public double PeakAmplitudeLinear { get; set; }
    public double ThdPercent { get; set; }
    public double ThdDb { get; set; }
    public double FundamentalFrequency { get; set; }
    public HarmonicData[]? Harmonics { get; set; }
}

public class HarmonicData
{
    public int HarmonicNumber { get; set; }
    public double Frequency { get; set; }
    public double MagnitudeLinear { get; set; }
    public double MagnitudeDb { get; set; }
}
