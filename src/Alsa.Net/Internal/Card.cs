using System.Runtime.InteropServices;

namespace Alsa.Net.Internal
{
    /// <summary>
    /// Represents an ALSA sound card.
    /// </summary>
    public class Card
    {
        private int _id;
        private string _name;

        /// <summary>
        /// Creates a new instance of the Card class.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        public Card(int id, string name)
        {
            _id = id;
            _name = name;
        }
        /// <summary>
        /// Id of the sound card.
        /// </summary>
        public int Id => _id;
        /// <summary>
        /// Name of the sound card.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Enumerate mixer controls for this card using libasound APIs.
        /// Returns an array of MixerControlInfo with basic channel raw values and ranges.
        /// </summary>
        public MixerControlInfo[] GetMixerControls()
        {
            var list = new List<MixerControlInfo>();
            IntPtr mixer = IntPtr.Zero;
            try
            {
                // open mixer
                int rc = InteropAlsa.snd_mixer_open(out mixer, 0);
                if (rc < 0) return Array.Empty<MixerControlInfo>();

                // Prefer attaching mixer by card short name when available
                // (e.g. "hw:CARD=IQaudIOCODEC"). If we don't have a name,
                // attach by numeric id (e.g. "hw:0"). Using the short name
                // matches what `aplay -L` shows on many systems.
                string attachName;
                if (!string.IsNullOrEmpty(_name))
                {
                    attachName = $"hw:CARD={_name}";
                }
                else
                {
                    attachName = $"hw:{_id}";
                }
                rc = InteropAlsa.snd_mixer_attach(mixer, attachName);
                if (rc < 0) return Array.Empty<MixerControlInfo>();

                rc = InteropAlsa.snd_mixer_selem_register(mixer, IntPtr.Zero, IntPtr.Zero);
                if (rc < 0) return Array.Empty<MixerControlInfo>();

                rc = InteropAlsa.snd_mixer_load(mixer);
                if (rc < 0) return Array.Empty<MixerControlInfo>();

                var elem = InteropAlsa.snd_mixer_first_elem(mixer);
                while (elem != IntPtr.Zero)
                {
                    var namePtr = InteropAlsa.snd_mixer_selem_get_name(elem);
                    string controlName = namePtr != IntPtr.Zero ? Marshal.PtrToStringUTF8(namePtr) ?? string.Empty : string.Empty;
                    var channels = new List<MixerControlChannelInfo>();

                    // check common channels (front left/right and mono)
                    var channelIds = new[] { snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_LEFT, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_RIGHT, snd_mixer_selem_channel_id.SND_MIXER_SCHN_MONO };
                    foreach (var ch in channelIds)
                    {
                        try
                        {
                            int hasPlayback = InteropAlsa.snd_mixer_selem_has_playback_channel(elem, ch);
                            if (hasPlayback <= 0) continue;

                            unsafe
                            {
                                nint raw = 0;
                                nint min = 0;
                                nint max = 0;

                                rc = InteropAlsa.snd_mixer_selem_get_playback_volume(elem, ch, &raw);
                                // get range
                                rc = InteropAlsa.snd_mixer_selem_get_playback_volume_range(elem, &min, &max);

                                channels.Add(new MixerControlChannelInfo(ch.ToString(), raw, min, max, null, null));
                            }
                        }
                        catch
                        {
                            // ignore channel read errors and continue
                        }
                    }

                    list.Add(new MixerControlInfo(controlName, channels.ToArray()));

                    elem = InteropAlsa.snd_mixer_elem_next(elem);
                }
            }
            catch
            {
                // on any error return what we have (could be empty)
            }
            finally
            {
                if (mixer != IntPtr.Zero)
                {
                    try { InteropAlsa.snd_mixer_close(mixer); } catch { }
                }
            }

            return list.ToArray();
        }
    }
}
