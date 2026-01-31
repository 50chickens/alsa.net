namespace Example.SNRReduction.Models;

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
    public HarmonicData[] Harmonics { get; set; } = Array.Empty<HarmonicData>();
    public double FundamentalFrequency { get; set; }
}

public class HarmonicData
{
    public int HarmonicNumber { get; set; }
    public double Frequency { get; set; }
    public double MagnitudeDb { get; set; }
    public double MagnitudeLinear { get; set; }
}
