using System.Runtime.InteropServices;
using AlsaSharp.Internal;

namespace AlsaSharp;

/// <summary>
/// connect your sound device configuration to a virtual interface
/// </summary>
public static class UnixSoundDeviceBuilder
{
    /// <summary>
    /// create and connect a sound device.
    /// use <see cref="SoundDeviceSettings"/> for parameter set up.
    /// </summary>
    /// <param name="settings">sound device configuration to use</param>
    /// <returns>sound device ready to use</returns>
    //public static ISoundDevice Build(SoundDeviceSettings settings) => new UnixSoundDevice(settings);

    /// <summary>
    /// create and connect a sound device from an alsa card
    /// </summary>
    /// <param name="card">the alsa card to use</param>
    /// <returns>sound device ready to use</returns>
    public static IEnumerable<ISoundDevice> Build()
    {
        
        // var soundDeviceSettings = new SoundDeviceSettings
        // {
        //     RecordingDeviceName = $"hw:CARD={soundDeviceOptions.RecordingDeviceName}",
        //     MixerDeviceName = $"hw:CARD={soundDeviceOptions.MixerDeviceName}",
        //     PlaybackDeviceName = $"hw:CARD={soundDeviceOptions.PlaybackDeviceName}"
        // };
        // return new UnixSoundDevice(soundDeviceSettings);
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
                //create a new UnixSoundDevice for each card found
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
                // free memory allocated by the ALSA helper
                if (namePtr != IntPtr.Zero)
                    InteropAlsa.free(namePtr);
            }

            returnCode = InteropAlsa.snd_card_next(ref cardIndex);
            if (returnCode < 0)
                throw new InvalidOperationException($"snd_card_next failed: {InteropAlsa.StrError(returnCode)}");
        }

        return list;
    }

    // public static ISoundDevice Build(SoundDeviceSettings soundSettings)
    // {
    //     return new UnixSoundDevice(soundSettings);
    // }
}

// public class SoundDeviceOptions
// {
//     public string RecordingDeviceName { get; set; } = "default";
//     public string MixerDeviceName { get; set; } = "default";
//     public string PlaybackDeviceName { get; set; } = "default";
// }