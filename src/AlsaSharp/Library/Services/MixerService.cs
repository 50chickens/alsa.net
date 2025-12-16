using AlsaSharp.Core.Alsa;
using AlsaSharp.Core.Native;
using System.Runtime.InteropServices;

namespace AlsaSharp.Library.Services
{
    /// <summary>
    /// Encapsulates logic to set mixer control values (volume/switch) on elements.
    /// </summary>
    public class MixerService
    {
        private readonly Microsoft.Extensions.Logging.ILogger<MixerService>? _log;

        public MixerService(Microsoft.Extensions.Logging.ILogger<MixerService>? log = null)
        {
            _log = log;
        }
        /// <summary>Attempts to set playback volume on a mixer element.</summary>
        /// <returns>True on success, false otherwise.</returns>
        public bool TrySetPlaybackVolume(int card, string controlName, string channelName, nint value)
        {
            using var handle = new MixerHandle(card);
            if (!handle.IsOpen) return false;

            var elem = handle.FindElementByName(controlName);
            if (elem == IntPtr.Zero) return false;
            if (InteropAlsa.snd_mixer_selem_has_playback_volume(elem) == 0) return false;
            if (!Enum.TryParse<snd_mixer_selem_channel_id>(channelName, out var channel)) return false;

            int rc = InteropAlsa.snd_mixer_selem_set_playback_volume(elem, channel, value);
            return rc >= 0;
        }

        /// <summary>Attempts to set capture volume on a mixer element.</summary>
        /// <returns>True on success, false otherwise.</returns>
        public bool TrySetCaptureVolume(int card, string controlName, string channelName, nint value)
        {
            using var handle = new MixerHandle(card);
            if (!handle.IsOpen) return false;

            var elem = handle.FindElementByName(controlName);
            if (elem == IntPtr.Zero) return false;
            if (!Enum.TryParse<snd_mixer_selem_channel_id>(channelName, out var channel)) return false;
            if (InteropAlsa.snd_mixer_selem_has_capture_channel(elem, channel) == 0) return false;

            int rc = InteropAlsa.snd_mixer_selem_set_capture_volume(elem, channel, value);
            return rc >= 0;
        }

        /// <summary>Attempts to set the playback on/off switch for a mixer element.</summary>
        /// <returns>True on success, false otherwise.</returns>
        public bool TrySetPlaybackSwitch(int card, string controlName, string channelName, int state)
        {
            using var handle = new MixerHandle(card);
            if (!handle.IsOpen) return false;

            var elem = handle.FindElementByName(controlName);
            if (elem == IntPtr.Zero) return false;
            if (!Enum.TryParse<snd_mixer_selem_channel_id>(channelName, out var channel)) return false;

            int rc = InteropAlsa.snd_mixer_selem_set_playback_switch(elem, channel, state);
            return rc >= 0;
        }

        /// <summary>Attempts to set the capture on/off switch for a mixer element.</summary>
        /// <returns>True on success, false otherwise.</returns>
        public bool TrySetCaptureSwitch(int card, string controlName, string channelName, int state)
        {
            using var handle = new MixerHandle(card);
            if (!handle.IsOpen) return false;

            var elem = handle.FindElementByName(controlName);
            if (elem == IntPtr.Zero) return false;
            if (!Enum.TryParse<snd_mixer_selem_channel_id>(channelName, out var channel)) return false;

            int rc = InteropAlsa.snd_mixer_selem_set_capture_switch(elem, channel, state);
            return rc >= 0;
        }

        /// <summary>
        /// Try to resolve an enumerated item name to its numeric index for the given control.
        /// Returns true and sets <paramref name="index"/> when a matching item is found.
        /// </summary>
        public bool TryGetEnumItemIndex(int card, string controlName, string itemLabel, out int index)
        {
            index = -1;
            using var handle = new MixerHandle(card);
            if (!handle.IsOpen) return false;

            var elem = handle.FindElementByName(controlName);
            if (elem == IntPtr.Zero) return false;

            int enumCount = InteropAlsa.snd_mixer_selem_get_enum_items(elem);
            if (enumCount <= 0) return false;

            for (int ei = 0; ei < enumCount; ei++)
            {
                try
                {
                    var itemPtr = InteropAlsa.snd_mixer_selem_get_enum_item_name(elem, ei);
                    if (itemPtr != IntPtr.Zero)
                    {
                        var itemName = Marshal.PtrToStringUTF8(itemPtr) ?? string.Empty;
                        var candidate = itemLabel?.Trim().Trim('\'') ?? string.Empty;
                        if (string.Equals(itemName.Trim('\''), candidate, StringComparison.OrdinalIgnoreCase) || string.Equals(itemName, candidate, StringComparison.OrdinalIgnoreCase))
                        {
                            index = ei;
                            return true;
                        }
                    }
                }
                catch { }
            }

            return false;
        }

        /// <summary>
        /// Attempts to set an enumerated item on the given control by matching a human readable label.
        /// If <paramref name="channelName"/> is null the method will attempt to set the item for any available channels.
        /// Returns true when at least one channel was successfully updated.
        /// </summary>
        public bool TrySetEnumItemByLabel(int card, string controlName, string itemLabel, string? channelName = null)
        {
            bool anySet = false;
            using var handle = new MixerHandle(card);
            if (!handle.IsOpen) return false;

            var elem = handle.FindElementByName(controlName);
            if (elem == IntPtr.Zero) return false;

            int enumCount = InteropAlsa.snd_mixer_selem_get_enum_items(elem);
            if (enumCount <= 0) return false;

            int foundIndex = -1;
            for (int ei = 0; ei < enumCount; ei++)
            {
                try
                {
                    var itemPtr = InteropAlsa.snd_mixer_selem_get_enum_item_name(elem, ei);
                    if (itemPtr == IntPtr.Zero) continue;
                    var itemName = Marshal.PtrToStringUTF8(itemPtr) ?? string.Empty;
                    var candidate = itemLabel?.Trim().Trim('\'') ?? string.Empty;
                    if (string.Equals(itemName.Trim('\''), candidate, StringComparison.OrdinalIgnoreCase) || string.Equals(itemName, candidate, StringComparison.OrdinalIgnoreCase))
                    {
                        foundIndex = ei;
                        break;
                    }
                }
                catch { }
            }

            if (foundIndex < 0) return false;

            // If a specific channel was requested, try parse and set only that channel.
            if (!string.IsNullOrWhiteSpace(channelName))
            {
                if (Enum.TryParse<snd_mixer_selem_channel_id>(channelName, out var channel))
                {
                    int rc = AlsaSharp.Library.Alsa.NativeMethods.snd_mixer_selem_set_enum_item(elem, channel, (uint)foundIndex);
                    return rc >= 0;
                }
                return false;
            }

            // No specific channel requested: attempt common relevant channels only to avoid invalid channel values.
            var candidateChannels = new[] {
                snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_LEFT,
                snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_RIGHT,
                snd_mixer_selem_channel_id.SND_MIXER_SCHN_MONO
            };

            foreach (var channel in candidateChannels)
            {
                try
                {
                    bool hasPlayback = InteropAlsa.snd_mixer_selem_has_playback_channel(elem, channel) != 0;
                    bool hasCapture = InteropAlsa.snd_mixer_selem_has_capture_channel(elem, channel) != 0;
                    if (!hasPlayback && !hasCapture) continue;
                    int rc = AlsaSharp.Library.Alsa.NativeMethods.snd_mixer_selem_set_enum_item(elem, channel, (uint)foundIndex);
                    if (rc >= 0) anySet = true;
                }
                catch { }
            }

            return anySet;
        }

        /// <summary>
        /// Attempts to read the current value of the named mixer control in a generic way.
        /// For ENUMERATED controls returns type="enum" and value=<human-label>.
        /// For integer volume controls returns type="integer" and value=<numeric(s)>.
        /// For switch controls returns type="switch" and value=<0|1 or on|off>.
        /// </summary>
        public bool TryGetElementValue(int card, string controlName, out string type, out string value)
        {
            type = "unknown";
            value = string.Empty;

            using var handle = new MixerHandle(card);
            if (!handle.IsOpen) return false;

            var elem = handle.FindElementByName(controlName);
            if (elem == IntPtr.Zero) return false;

            try
            {
                int enumCount = InteropAlsa.snd_mixer_selem_get_enum_items(elem);
                if (enumCount > 0)
                {
                    // Try mono first, then front left/right
                    var channels = new[] { snd_mixer_selem_channel_id.SND_MIXER_SCHN_MONO, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_LEFT, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_RIGHT };
                    foreach (var ch in channels)
                    {
                        if (InteropAlsa.snd_mixer_selem_get_enum_item(elem, ch, out uint idx) >= 0)
                        {
                            var ptr = InteropAlsa.snd_mixer_selem_get_enum_item_name(elem, (int)idx);
                            var lbl = ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(ptr) ?? string.Empty : idx.ToString();
                            type = "enum";
                            value = lbl.Trim('\'');
                            return true;
                        }
                    }
                }

                // Fallback: try to read playback volume for front-left and front-right
                try
                {
                    unsafe
                    {
                        nint left = 0, right = 0;
                        if (InteropAlsa.snd_mixer_selem_get_playback_volume(elem, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_LEFT, &left) == 0 &&
                            InteropAlsa.snd_mixer_selem_get_playback_volume(elem, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_RIGHT, &right) == 0)
                        {
                            type = "integer";
                            value = $"{left},{right}";
                            return true;
                        }
                    }
                }
                catch { }

                // Try capture switch/presence as switch
                try
                {
                    unsafe
                    {
                        int v = 0;
                        if (InteropAlsa.snd_mixer_selem_get_playback_switch(elem, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_LEFT, &v) == 0)
                        {
                            type = "switch";
                            value = v != 0 ? "1" : "0";
                            return true;
                        }
                    }
                }
                catch { }
            }
            catch { }

            return false;
        }
    }
}
