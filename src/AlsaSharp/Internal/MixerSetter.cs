using System;
using System.Runtime.InteropServices;

namespace AlsaSharp.Internal
{
    /// <summary>
    /// Encapsulates logic to set mixer control values (volume/switch) on elements.
    /// </summary>
    public class MixerSetter
    {
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
    }
}
