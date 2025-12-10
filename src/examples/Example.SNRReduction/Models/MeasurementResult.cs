using Example.SNRReduction.Audio;

namespace Example.SNRReduction.Models;

public class MeasurementResult
{
    public MeasurementResult(DateTime generatedUtc, DateTime startedUtc, TimeSpan totalDuration, TimeSpan interval, string description)
    {
        GeneratedUtc = generatedUtc;
        StartedUtc = startedUtc;
        TotalDuration = totalDuration;
        Interval = interval;
        Description = description;
        MeasureWindow = interval;
        Readings = new List<AudioMeterLevelReading>();
    }

    public DateTime GeneratedUtc { get; set; }
    public DateTime StartedUtc { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan Interval { get; set; }
    public TimeSpan MeasureWindow { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<AudioMeterLevelReading> Readings { get; set; } = new List<AudioMeterLevelReading>();
}
