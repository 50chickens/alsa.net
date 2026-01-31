#nullable enable

using AlsaSharp;

namespace Example.SNRReduction.Services;

public interface IAudioCardSelector
{
    IEnumerable<ISoundDevice> SelectCards(IEnumerable<ISoundDevice> allDevices, string selector);
}
