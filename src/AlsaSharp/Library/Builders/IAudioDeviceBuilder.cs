namespace AlsaSharp.Library.Builders;

public interface IAudioDeviceBuilder
{
    public IEnumerable<ISoundDevice> BuildAudioDevices();
}
