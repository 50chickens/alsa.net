namespace Example.SNRReduction.Models;

public class SNRReductionServiceOptions
{
    public const string Settings = "SNRReduction";
    public bool AutoSweep { get; set; } = false;
    public bool BaselineOnly { get; set; } = true;
    public bool ApplyAlsaStateFile { get; set; } = false;
    public string DefaultAudioStateFolderName { get; set; } = "~/pi-stomp/setup/audio";
    /// <summary>
    /// When true the worker will attempt to programmatically set a sane DAI Left Source MUX
    /// (e.g. try ADC Right/ADC Left) before taking measurements. Default is false.
    /// </summary>
    public bool AutoConfigureDaiMux { get; set; } = false;
    public bool MeasureSNR { get; set; } = false;
    public bool MeasureAudioLevels { get; set; } = true;
    public string ApplyAlsaState { get; internal set; } = string.Empty;
}
