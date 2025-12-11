namespace Example.SNRReduction.Models;
public class ControlSweepConfigurationOption
{
    public string ControlName { get; set; } = string.Empty;
    public int StartValue { get; set; }
    public int EndValue { get; set; }
    public int Step { get; set; }
}