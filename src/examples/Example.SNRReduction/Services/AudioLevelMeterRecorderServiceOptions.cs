namespace Example.SNRReduction.Services;

/// <summary>
/// Options for the audio level meter recorder service.
/// </summary>
public class AudioLevelMeterRecorderServiceOptions(int measurementDuration, int measurementCount, string description)
{
    private readonly int _measurementDuration = measurementDuration;
    private readonly int _measurementCount = measurementCount;
    private readonly string _description = description ?? throw new ArgumentNullException("Description cannot be null");
    public const string Settings = "AudioLevelMeterRecorderService";
    public int MeasurementDuration { get => _measurementDuration; init => _measurementDuration = value; }
    public int MeasurementCount { get => _measurementCount; init => _measurementCount = value; }
    public string Description { get => _description; init => _description = value ?? throw new ArgumentNullException("Description cannot be null"); }
}