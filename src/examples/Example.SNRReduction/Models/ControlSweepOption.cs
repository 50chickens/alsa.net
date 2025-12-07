using System.Reflection.Metadata.Ecma335;

namespace Example.SNRReduction.Models;

public class ControlSweepOption
{
    public string ControNameRegex { get; set; } = "ADC";
    public int ClampMinValue { get; set; } = 0;
    public int ClampMaxValue { get; set; } = 100;
}

public class ControlSweepOptions(List<AlsaControl> controls)
{
    
    public const string Settings = "ControlSweep";
    private readonly List<AlsaControl> _controls = controls;
    public IEnumerable<ControlSweepOption> ControlSweeps 
    {
        get
        {
            var sweepOptions = new List<ControlSweepOption>();
            foreach (var control in _controls)
            {
                sweepOptions.Add(new ControlSweepOption
                {
                    ControNameRegex = control.Name,
                    ClampMinValue = control.MinValue,
                    ClampMaxValue = control.MaxValue
                });
            }
            return sweepOptions;
        }
    }

}

/// <summary>
/// Represents a control on an ALSA audio device.
/// </summary>
public class AlsaControl
{
    public string Name { get; set; } = string.Empty;
    public int MinValue { get; set; }
    public int MaxValue { get; set; }
}