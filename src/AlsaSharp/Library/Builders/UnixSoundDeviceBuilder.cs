using AlsaSharp.Core.Native;
using System.Runtime.InteropServices;

namespace AlsaSharp.Library.Builders;

/// <summary>
/// Connect your sound device configuration to a virtual interface.
/// </summary>
public class UnixSoundDeviceBuilder
{
    /// <summary>
    /// Create and connect a sound device for each discovered ALSA card.
    /// </summary>
    /// <returns>Sound device instances ready to use.</returns>
    public static IEnumerable<ISoundDevice> Build()
    {
        return GetSoundDevices();
    }
    private static IEnumerable<ISoundDevice> GetSoundDevices()
    {
        var list = new List<ISoundDevice>();
        int cardIndex = -1;

        // Start enumeration
        var returnCode = InteropAlsa.snd_card_next(ref cardIndex);
        if (returnCode < 0)
            throw new InvalidOperationException($"snd_card_next failed: {InteropAlsa.StrError(returnCode)}");

        while (cardIndex >= 0)
        {
            // Obtain the short name for the card via the proper libasound API.
            IntPtr namePtr = IntPtr.Zero;
            returnCode = InteropAlsa.snd_card_get_name(cardIndex, out namePtr);
            if (returnCode < 0)
                throw new InvalidOperationException($"snd_card_get_name({cardIndex}) failed: {InteropAlsa.StrError(returnCode)}");

            try
            {
                var name = Marshal.PtrToStringUTF8(namePtr);
                if (name == null) throw new InvalidOperationException($"Could not retrieve card name for index {cardIndex}");
                // Create a new UnixSoundDevice for each card found
                var soundDeviceSettings = new SoundDeviceSettings
                {
                    RecordingDeviceName = $"hw:CARD={name}",
                    MixerDeviceName = $"hw:CARD={name}",
                    PlaybackDeviceName = $"hw:CARD={name}"
                };
                var soundDevice = new UnixSoundDevice(soundDeviceSettings);
                list.Add(soundDevice);
            }
            finally
            {
                // Free memory allocated by the ALSA helper
                if (namePtr != IntPtr.Zero)
                    InteropAlsa.free(namePtr);
            }

            returnCode = InteropAlsa.snd_card_next(ref cardIndex);
            if (returnCode < 0)
                throw new InvalidOperationException($"snd_card_next failed: {InteropAlsa.StrError(returnCode)}");
        }

        return list;
    }
}