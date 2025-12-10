namespace Example.SNRReduction.Services;

/// <summary>
/// Control level for a mixer channel.
/// </summary>
public class ControlLevel(string controlName, string channelName, long value)
{
    private readonly string _controlName = controlName ?? throw new ArgumentNullException("ControlName cannot be null");
    private readonly string _channelName = channelName ?? throw new ArgumentNullException("ChannelName cannot be null");
    private readonly long _value = value;
    public string ControlName { get => _controlName; init => _controlName = value ?? throw new ArgumentNullException("ControlName cannot be null"); }
    public string ChannelName { get => _channelName; init => _channelName = value ?? throw new ArgumentNullException("ChannelName cannot be null"); }
    public long Value { get => _value; init => _value = value; }
}