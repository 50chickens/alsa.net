namespace AlsaSharp.Library.Operations.Services;

public class SNRAnalysisResult
{
    public double AverageSnrDb { get; set; }
    public int CleanSections { get; set; }
    public int NoiseSections { get; set; }
    public List<double> SectionSnrDb { get; set; } = new List<double>();
    public int Frames { get; set; }
    public int Channels { get; set; }
    public int BytesPerSample { get; set; }
    public int SampleRate { get; set; }
    public double AverageSignalDb { get; set; }
    public double AverageNoiseDb { get; set; }
    public double AverageOutputDb { get; set; }
    public double TotalHarmonicDistortionDb { get; set; }
    public string AverageSnrUnit { get; set; } = "decibels (dB)";
    public string SectionSnrUnit { get; set; } = "decibels (dB)";
    public string FramesUnit { get; set; } = "frames";
    public string ChannelsUnit { get; set; } = "count";
    public string BytesPerSampleUnit { get; set; } = "bytes";
    public string SampleRateUnit { get; set; } = "hertz (Hz)";
    public string AverageSignalUnit { get; set; } = "decibels (dB)";
    public string AverageNoiseUnit { get; set; } = "decibels (dB)";
    public string AverageOutputUnit { get; set; } = "decibels (dB)";
    public string TotalHarmonicDistortionUnit { get; set; } = "decibels (dB)";
}
