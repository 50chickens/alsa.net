namespace Examples.SNRReduction.Services;

public class SNRSweepResult
{
    public string ControlName { get; init; } = string.Empty;
    public string ChannelName { get; init; } = string.Empty;
    public long Value { get; init; }
    public double SignalRms { get; init; }
    public double NoiseRms { get; init; }
    public double SNRdB { get; init; }
}
