using System.ComponentModel.DataAnnotations;

namespace Example.SNRReduction.Models;

public class SNRReductionServiceOptions
{
    public const string Settings = "SNRReduction";
    public bool AutoSweep { get; set; } = false;
    public bool BaselineOnly { get; set; } = true;
    public int MeasurementDuration { get; set; } = 60;
    // Terminal GUI mode: when true the app runs an interactive console status view instead of the sweep.
    public bool TerminalGui { get; set; } = false;
    // Poll interval in seconds for Terminal GUI mode.
    public int GuiIntervalSeconds { get; set; } = 1;
    public bool RestoreAlsaStateBeforeMeasurement { get; set; } = false;
    public string DefaultAudioStateFolderName { get; set; } = "~/pi-stomp/setup/audio";
    public string DefaultAudioStateFileName { get; set; } = "iqaudiocodec.state";
}
