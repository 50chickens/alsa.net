using System.ComponentModel.DataAnnotations;

namespace Example.SNRReduction.Models;

public class SNRReductionServiceOptions
{
    public const string Settings = "SNRReduction";
    public bool AutoSweep { get; set; } = false;
    public bool BaselineOnly { get; set; } = true;
    // If non-empty, this value is either a full path to an alsactl-style state file
    // or a simple filename which will be resolved inside DefaultAudioStateFolderName.
    // When supplied, the worker will apply the file by calling the library restore
    // routine on each discovered device (no external alsactl invocation).
    public string ApplyAlsaStateFile { get; set; } = string.Empty;
    public string DefaultAudioStateFolderName { get; set; } = "~/pi-stomp/setup/audio";
    /// <summary>
    /// Folder to write measurement JSON files to. Supports ~ for the user's home folder.
    /// </summary>
    public string MeasurementFolder { get; set; } = "~/.SNRReduction";
    /// <summary>
    /// When true the worker will attempt to programmatically set a sane DAI Left Source MUX
    /// (e.g. try ADC Right/ADC Left) before taking measurements. Default is false.
    /// </summary>
    public bool AutoConfigureDaiMux { get; set; } = false;
    public bool MeasureSNR { get; set; } = true;
    public bool MeasureAudioLevels { get; set; } = false;
}
