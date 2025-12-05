using Example.SNRReduction.Audio;

namespace Example.SNRReduction.Models;

public class MeasurementResult(DateTime generatedUtc, DateTime startedUtc, TimeSpan totalDuration, TimeSpan interval, string description)
{
    public DateTime GeneratedUtc { get; set; }
    public DateTime StartedUtc { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan Interval { get; set; }
    public TimeSpan MeasureWindow { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<AudioMeterLevelReading> Readings { get; set; } = new List<AudioMeterLevelReading>();
}
