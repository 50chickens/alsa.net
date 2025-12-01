using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Alsa.Net.Internal
{
    /// <summary>
    /// Enumerates mixer controls for a card and maps them to <see cref="MixerControlInfo"/> objects.
    /// </summary>
    public class MixerEnumerator
    {
        public MixerControlInfo[] GetControlsForCard(int card)
        {
            var controls = new List<MixerControlInfo>();
            using var handle = new MixerHandle(card);
            if (!handle.IsOpen) return Array.Empty<MixerControlInfo>();

            var elem = handle.FirstElem();
            while (elem != IntPtr.Zero)
            {
                var namePtr = InteropAlsa.snd_mixer_selem_get_name(elem);
                string name = namePtr != IntPtr.Zero ? Marshal.PtrToStringUTF8(namePtr) ?? string.Empty : string.Empty;
                var channels = new List<MixerControlChannelInfo>();

                // Probe up to 2 channel ids (front left / front right) — this matches previous behavior.
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
                                if (InteropAlsa.snd_mixer_selem_get_playback_volume(elem, channelId, &v) >= 0) raw = v;
                                nint mn = 0, mx = 0;
                                if (InteropAlsa.snd_mixer_selem_get_playback_volume_range(elem, &mn, &mx) >= 0) { min = mn; max = mx; }
                            }

                            long dbv = 0;
                            if (InteropAlsa.snd_mixer_selem_get_playback_dB(elem, channelId, &dbv) >= 0) db = dbv;

                            int sw = 0;
                            if (InteropAlsa.snd_mixer_selem_get_playback_switch(elem, channelId, &sw) >= 0) swState = sw;
                        }
                        else if (hasCapture != 0)
                        {
                            nint v = 0;
                            if (InteropAlsa.snd_mixer_selem_get_capture_volume(elem, channelId, &v) >= 0) raw = v;
                            nint mn = 0, mx = 0;
                            if (InteropAlsa.snd_mixer_selem_get_capture_volume_range(elem, &mn, &mx) >= 0) { min = mn; max = mx; }
                            long dbv = 0;
                            if (InteropAlsa.snd_mixer_selem_get_capture_dB(elem, channelId, &dbv) >= 0) db = dbv;
                            int sw = 0;
                            if (InteropAlsa.snd_mixer_selem_get_capture_switch(elem, channelId, &sw) >= 0) swState = sw;
                        }
                    }

                    channels.Add(new MixerControlChannelInfo(channelId.ToString(), raw, min, max, db, swState));
                }

                if (channels.Count > 0) controls.Add(new MixerControlInfo(name, channels.ToArray()));
                elem = handle.NextElem(elem);
            }

            return controls.ToArray();
        }
    }
}
