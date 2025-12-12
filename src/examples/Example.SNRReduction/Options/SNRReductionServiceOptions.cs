using System.ComponentModel.DataAnnotations;

namespace Example.SNRReduction.Models;

public class SNRReductionServiceOptions
{
    public const string Settings = "SNRReduction";
    public bool AutoSweep { get; set; } = false;
    public bool BaselineOnly { get; set; } = true;
    public bool RestoreAlsaStateBeforeMeasurement { get; set; } = false;
    public string DefaultAudioStateFolderName { get; set; } = "~/pi-stomp/setup/audio";
    public string DefaultAudioStateFileName { get; set; } = "iqaudiocodec.state";
}
