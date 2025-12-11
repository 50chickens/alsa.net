namespace Example.SNRReduction.Models;

/// <summary>
/// Sweep service options for control/channel configuration.
/// </summary>
public class SweepServiceOptions(List<ControlSweepConfigurationOption> controls, List<string> channels)
{
    private readonly List<ControlSweepConfigurationOption> _controls = controls ?? new List<ControlSweepConfigurationOption>();
    private readonly List<string> _channels = channels ?? new List<string>();
    public const string Settings = "SweepService";
    public List<ControlSweepConfigurationOption> Controls { get => _controls; init => _controls = value ?? new List<ControlSweepConfigurationOption>(); }
    public List<string> Channels { get => _channels; init => _channels = value ?? new List<string>(); }
}
