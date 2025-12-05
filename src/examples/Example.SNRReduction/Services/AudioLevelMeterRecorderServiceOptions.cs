namespace Examples.SNRReduction.Services;

public class AudioLevelMeterRecorderServiceOptions()
{
    public const string Settings = "AudioLevelMeterRecorderService";
    public int MeasurementDuration { get; set; } = 3;
    public int MeasurementCount { get; set; } = 10;
    public string Description { get; set; } = "";
}