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
    // Units (long descriptive names matching alsabat conventions)
    // Timestamp is expressed in Coordinated Universal Time
    public string TimestampUnit { get; set; } = "UTC";
    // Durations / windows are expressed in seconds
    public string TotalDurationUnit { get; set; } = "seconds";
    public string IntervalUnit { get; set; } = "seconds";
    public string MeasureWindowUnit { get; set; } = "seconds";
    // Per-channel level units: decibels relative to full scale
    public string ChannelDbfsUnit { get; set; } = "decibels relative to full scale (dBFS)";
    // Per-channel RMS values: root-mean-square (linear, full-scale = 1.0)
    public string ChannelRmsUnit { get; set; } = "root-mean-square (linear, full-scale=1.0)";
    // Total harmonic distortion for the measurement (if available)
    // Unit: decibels (dB) — matches the analyzer's THD output
    public double? TotalHarmonicDistortionDb { get; set; }
    public string TotalHarmonicDistortionUnit { get; set; } = "decibels (dB)";
    public List<AudioMeterLevelReading> Readings { get; set; } = new List<AudioMeterLevelReading>();
}
