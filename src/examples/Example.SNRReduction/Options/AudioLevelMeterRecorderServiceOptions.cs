namespace Example.SNRReduction.Services;

/// <summary>
/// Options for the audio level meter recorder service.
/// </summary>
public class AudioLevelMeterRecorderServiceOptions
{
    public const string Settings = "AudioLevelMeterRecorderService";

    // Parameterless constructor required for configuration binder / OptionsFactory
    public AudioLevelMeterRecorderServiceOptions()
    {
        MeasurementDuration = 3;
        MeasurementCount = 1;
        Description = "Baseline recording";
    }

    // Optional convenience constructor
    public AudioLevelMeterRecorderServiceOptions(int measurementDuration, int measurementCount, string description)
    {
        MeasurementDuration = measurementDuration;
        MeasurementCount = measurementCount;
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    public int MeasurementDuration { get; set; }
    public int MeasurementCount { get; set; }
    public string Description { get; set; } = string.Empty;
}