namespace Example.SNRReduction.Services;

/// <summary>
/// SNR sweep result for a control/channel.
/// </summary>
public class SNRSweepResult(string controlName, string channelName, long value, double signalRms, double noiseRms, double snrDb)
{
    private readonly string _controlName = controlName ?? throw new ArgumentNullException("ControlName cannot be null");
    private readonly string _channelName = channelName ?? throw new ArgumentNullException("ChannelName cannot be null");
    private readonly long _value = value;
    private readonly double _signalRms = signalRms;
    private readonly double _noiseRms = noiseRms;
    private readonly double _snrDb = snrDb;
    public string ControlName { get => _controlName; init => _controlName = value ?? throw new ArgumentNullException("ControlName cannot be null"); }
    public string ChannelName { get => _channelName; init => _channelName = value ?? throw new ArgumentNullException("ChannelName cannot be null"); }
    public long Value { get => _value; init => _value = value; }
    public double SignalRms { get => _signalRms; init => _signalRms = value; }
    public double NoiseRms { get => _noiseRms; init => _noiseRms = value; }
    public double SNRdB { get => _snrDb; init => _snrDb = value; }
}
