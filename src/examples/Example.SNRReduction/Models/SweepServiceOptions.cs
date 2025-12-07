namespace Example.SNRReduction.Models;

public class SweepServiceOptions
{
    public const string Settings = "SweepService";
    public List<ControlSweepConfigurationOption> Controls { get; set; } = new List<ControlSweepConfigurationOption>();
    public List<string> Channels { get; set; } = new List<string>();
}
