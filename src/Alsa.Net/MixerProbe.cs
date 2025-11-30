// MixerProbe: probe ALSA mixer controls and provide simple setter helpers.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Alsa.Net.Internal;

namespace Alsa.Net
{
    /// <summary>
    /// Probes the system mixer and returns control information for a given card
    /// and provides simple setter helpers for use by examples and tools.
    /// </summary>
    public class MixerProbe
    {
        // Simple helper: enumerate controls and return basic info.
        public MixerControlInfo[] GetControlsForCard(int card)
        {
            var controls = new List<MixerControlInfo>();
            IntPtr mixer = IntPtr.Zero;
            try
            {
                if (InteropAlsa.snd_mixer_open(out mixer, 0) < 0) return Array.Empty<MixerControlInfo>();
                var attachName = $"hw:{card}";
                if (InteropAlsa.snd_mixer_attach(mixer, attachName) < 0) return Array.Empty<MixerControlInfo>();
                if (InteropAlsa.snd_mixer_selem_register(mixer, IntPtr.Zero, IntPtr.Zero) < 0) return Array.Empty<MixerControlInfo>();
                if (InteropAlsa.snd_mixer_load(mixer) < 0) return Array.Empty<MixerControlInfo>();

                var elem = InteropAlsa.snd_mixer_first_elem(mixer);
                while (elem != IntPtr.Zero)
                {
                    var namePtr = InteropAlsa.snd_mixer_selem_get_name(elem);
                    string name = namePtr != IntPtr.Zero ? Marshal.PtrToStringUTF8(namePtr) ?? string.Empty : string.Empty;
                    var channels = new List<MixerControlChannelInfo>();

                    for (int ch = 0; ch < 2; ch++)
                    {
                        var channelId = (snd_mixer_selem_channel_id)ch;
                        int hasPlayback = InteropAlsa.snd_mixer_selem_has_playback_channel(elem, channelId);
                        int hasCapture = InteropAlsa.snd_mixer_selem_has_capture_channel(elem, channelId);
                        if (hasPlayback == 0 && hasCapture == 0) continue;

                        nint raw = 0, min = 0, max = 0;
                        long? db = null;
                        int? swState = null;

                        unsafe
                        {
                            if (hasPlayback != 0)
                            {
                                if (InteropAlsa.snd_mixer_selem_has_playback_volume(elem) != 0)
                                {
                                    nint v = 0;
                                    int rc = InteropAlsa.snd_mixer_selem_get_playback_volume(elem, channelId, &v);
                                    if (rc >= 0) raw = v;
                                    nint mn = 0, mx = 0;
                                    rc = InteropAlsa.snd_mixer_selem_get_playback_volume_range(elem, &mn, &mx);
                                    if (rc >= 0) { min = mn; max = mx; }
                                }

                                long dbv = 0;
                                int rcdb = InteropAlsa.snd_mixer_selem_get_playback_dB(elem, channelId, &dbv);
                                if (rcdb >= 0) db = dbv;

                                int sw = 0;
                                int rcsw = InteropAlsa.snd_mixer_selem_get_playback_switch(elem, channelId, &sw);
                                if (rcsw >= 0) swState = sw;
                            }
                            else if (hasCapture != 0)
                            {
                                nint v = 0;
                                int rc = InteropAlsa.snd_mixer_selem_get_capture_volume(elem, channelId, &v);
                                if (rc >= 0) raw = v;
                                nint mn = 0, mx = 0;
                                rc = InteropAlsa.snd_mixer_selem_get_capture_volume_range(elem, &mn, &mx);
                                if (rc >= 0) { min = mn; max = mx; }
                                long dbv = 0;
                                rc = InteropAlsa.snd_mixer_selem_get_capture_dB(elem, channelId, &dbv);
                                if (rc >= 0) db = dbv;
                                int sw = 0;
                                rc = InteropAlsa.snd_mixer_selem_get_capture_switch(elem, channelId, &sw);
                                if (rc >= 0) swState = sw;
                            }
                        }

                        channels.Add(new MixerControlChannelInfo(channelId.ToString(), raw, min, max, db, swState));
                    }

                    if (channels.Count > 0) controls.Add(new MixerControlInfo(name, channels.ToArray()));
                    elem = InteropAlsa.snd_mixer_elem_next(elem);
                }

                return controls.ToArray();
            }
            finally
            {
                if (mixer != IntPtr.Zero) InteropAlsa.snd_mixer_close(mixer);
            }
        }

        public bool TrySetPlaybackVolume(int card, string controlName, string channelName, nint value)
        {
            IntPtr mixer = IntPtr.Zero;
            try
            {
                if (InteropAlsa.snd_mixer_open(out mixer, 0) < 0) return false;
                var attachName = $"hw:{card}";
                if (InteropAlsa.snd_mixer_attach(mixer, attachName) < 0) return false;
                if (InteropAlsa.snd_mixer_selem_register(mixer, IntPtr.Zero, IntPtr.Zero) < 0) return false;
                if (InteropAlsa.snd_mixer_load(mixer) < 0) return false;

                var elem = InteropAlsa.snd_mixer_first_elem(mixer);
                while (elem != IntPtr.Zero)
                {
                    var namePtr = InteropAlsa.snd_mixer_selem_get_name(elem);
                    string name = namePtr != IntPtr.Zero ? Marshal.PtrToStringUTF8(namePtr) ?? string.Empty : string.Empty;
                    if (string.Equals(name, controlName, StringComparison.Ordinal))
                    {
                        if (InteropAlsa.snd_mixer_selem_has_playback_volume(elem) == 0) return false;
                        if (!Enum.TryParse<snd_mixer_selem_channel_id>(channelName, out var channel)) return false;
                        int rc = InteropAlsa.snd_mixer_selem_set_playback_volume(elem, channel, value);
                        return rc >= 0;
                    }

                    elem = InteropAlsa.snd_mixer_elem_next(elem);
                }

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (mixer != IntPtr.Zero) InteropAlsa.snd_mixer_close(mixer);
            }
        }

        public bool TrySetPlaybackSwitch(int card, string controlName, string channelName, int state)
        {
            IntPtr mixer = IntPtr.Zero;
            try
            {
                if (InteropAlsa.snd_mixer_open(out mixer, 0) < 0) return false;
                var attachName = $"hw:{card}";
                if (InteropAlsa.snd_mixer_attach(mixer, attachName) < 0) return false;
                if (InteropAlsa.snd_mixer_selem_register(mixer, IntPtr.Zero, IntPtr.Zero) < 0) return false;
                if (InteropAlsa.snd_mixer_load(mixer) < 0) return false;

                var elem = InteropAlsa.snd_mixer_first_elem(mixer);
                while (elem != IntPtr.Zero)
                {
                    var namePtr = InteropAlsa.snd_mixer_selem_get_name(elem);
                    string name = namePtr != IntPtr.Zero ? Marshal.PtrToStringUTF8(namePtr) ?? string.Empty : string.Empty;
                    if (string.Equals(name, controlName, StringComparison.Ordinal))
                    {
                        if (!Enum.TryParse<snd_mixer_selem_channel_id>(channelName, out var channel)) return false;
                        int rc = InteropAlsa.snd_mixer_selem_set_playback_switch(elem, channel, state);
                        return rc >= 0;
                    }

                    elem = InteropAlsa.snd_mixer_elem_next(elem);
                }

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (mixer != IntPtr.Zero) InteropAlsa.snd_mixer_close(mixer);
            }
        }

        public bool TrySetCaptureVolume(int card, string controlName, string channelName, nint value)
        {
            IntPtr mixer = IntPtr.Zero;
            try
            {
                if (InteropAlsa.snd_mixer_open(out mixer, 0) < 0) return false;
                var attachName = $"hw:{card}";
                if (InteropAlsa.snd_mixer_attach(mixer, attachName) < 0) return false;
                if (InteropAlsa.snd_mixer_selem_register(mixer, IntPtr.Zero, IntPtr.Zero) < 0) return false;
                if (InteropAlsa.snd_mixer_load(mixer) < 0) return false;

                var elem = InteropAlsa.snd_mixer_first_elem(mixer);
                while (elem != IntPtr.Zero)
                {
                    var namePtr = InteropAlsa.snd_mixer_selem_get_name(elem);
                    string name = namePtr != IntPtr.Zero ? Marshal.PtrToStringUTF8(namePtr) ?? string.Empty : string.Empty;
                    if (string.Equals(name, controlName, StringComparison.Ordinal))
                    {
                        if (!Enum.TryParse<snd_mixer_selem_channel_id>(channelName, out var channel)) return false;
                        if (InteropAlsa.snd_mixer_selem_has_capture_channel(elem, channel) == 0) return false;
                        int rc = InteropAlsa.snd_mixer_selem_set_capture_volume(elem, channel, value);
                        return rc >= 0;
                    }

                    elem = InteropAlsa.snd_mixer_elem_next(elem);
                }

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (mixer != IntPtr.Zero) InteropAlsa.snd_mixer_close(mixer);
            }
        }

        public bool TrySetCaptureSwitch(int card, string controlName, string channelName, int state)
        {
            IntPtr mixer = IntPtr.Zero;
            try
            {
                if (InteropAlsa.snd_mixer_open(out mixer, 0) < 0) return false;
                var attachName = $"hw:{card}";
                if (InteropAlsa.snd_mixer_attach(mixer, attachName) < 0) return false;
                if (InteropAlsa.snd_mixer_selem_register(mixer, IntPtr.Zero, IntPtr.Zero) < 0) return false;
                if (InteropAlsa.snd_mixer_load(mixer) < 0) return false;

                var elem = InteropAlsa.snd_mixer_first_elem(mixer);
                while (elem != IntPtr.Zero)
                {
                    var namePtr = InteropAlsa.snd_mixer_selem_get_name(elem);
                    string name = namePtr != IntPtr.Zero ? Marshal.PtrToStringUTF8(namePtr) ?? string.Empty : string.Empty;
                    if (string.Equals(name, controlName, StringComparison.Ordinal))
                    {
                        if (!Enum.TryParse<snd_mixer_selem_channel_id>(channelName, out var channel)) return false;
                        int rc = InteropAlsa.snd_mixer_selem_set_capture_switch(elem, channel, state);
                        return rc >= 0;
                    }

                    elem = InteropAlsa.snd_mixer_elem_next(elem);
                }

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (mixer != IntPtr.Zero) InteropAlsa.snd_mixer_close(mixer);
            }
        }
    }
}
