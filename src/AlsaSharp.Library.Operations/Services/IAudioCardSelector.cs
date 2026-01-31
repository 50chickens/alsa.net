using AlsaSharp;

namespace AlsaSharp.Library.Operations.Services;

public interface IAudioCardSelector
{
    IEnumerable<ISoundDevice> SelectCards(IEnumerable<ISoundDevice> allDevices, string selector);
}
