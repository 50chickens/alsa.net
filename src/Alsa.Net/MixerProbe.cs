using Alsa.Net.Internal;
using System.Runtime.InteropServices;

namespace Alsa.Net
{
    /// <summary>
    /// Probes the system mixer and returns control information for a given card.
    /// </summary>
    public class MixerProbe
    {
        /// <summary>
        /// Gets mixer control information for the specified card index.
        /// </summary>
        /// <param name="card">The card index (typically 0 for the first card).</param>
        /// <returns>An array of <see cref="MixerControlInfo"/> describing controls found on the card.</returns>
        public MixerControlInfo[] GetControlsForCard(int card)
        {
            var controls = new List<MixerControlInfo>();
            IntPtr mixer = IntPtr.Zero;
            Exception? primaryEx = null;
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
                                // Only query playback volume if the element supports it
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
            }
            catch (Exception ex)
            {
                primaryEx = ex;
                throw;
            }
            finally
            {
                if (mixer != IntPtr.Zero)
                {
                    int rc = InteropAlsa.snd_mixer_close(mixer);
                    if (rc < 0)
                    {
                        var closeEx = new InvalidOperationException($"snd_mixer_close failed: {InteropAlsa.StrError(rc)}");
                        if (primaryEx != null)
                            throw new AggregateException(primaryEx, closeEx);
                        else
                            throw closeEx;
                    }
                }
            }

            return controls.ToArray();
        }
    }
}
