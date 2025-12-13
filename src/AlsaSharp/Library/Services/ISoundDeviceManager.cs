using AlsaSharp.Core.Alsa;

namespace AlsaSharp.Library.Services
{
    /// <summary>
    /// Service interface for managing sound devices.
    /// </summary> 
    public interface ISoundDeviceManager
    {
        /// <summary>
        /// Get simple mixer elements for a given sound device.
        /// </summary>
        List<MixerSimpleElement> GetMixerSimpleElements(ISoundDevice soundDevice);
    }
}
