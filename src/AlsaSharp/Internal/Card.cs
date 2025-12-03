using AlsaSharp.Library.Logging;
using System.Runtime.InteropServices;

namespace AlsaSharp.Internal
{
    /// <summary>
    /// Represents an ALSA sound card.
    /// </summary>
    public class Card
    {
        private int _index;
        private string _name;
        private readonly ILog<Card> _log;

        /// <summary>
        /// Creates a new instance of the Card class.
        /// </summary>
        /// <param name="log">Logger instance scoped to this type.</param>
        /// <param name="index">Card numeric id.</param>
        /// <param name="name">Card short name.</param>
        public Card(ILog<Card> log, int index, string name)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _index = index;
            _name = name;
        }
        /// <summary>
        /// Id of the sound card.
        /// </summary>
        public int Index => _index;
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
            Exception? primaryEx = null;
            try
            {
                // open mixer
                int rc = InteropAlsa.snd_mixer_open(out mixer, 0);
                if (rc < 0) throw new InvalidOperationException($"snd_mixer_open failed: {InteropAlsa.StrError(rc)}");

                // Prefer attaching mixer by card short name when available
                // (e.g. "hw:CARD=IQaudIOCODEC"). If we don't have a name,
                // attach by numeric id (e.g. "hw:0"). Using the short name
                // matches what `aplay -L` shows on many systems.
                string attachName = !string.IsNullOrEmpty(_name) ? $"hw:CARD={_name}" : $"hw:{_index}";

                rc = InteropAlsa.snd_mixer_attach(mixer, attachName);
                if (rc < 0) throw new InvalidOperationException($"snd_mixer_attach failed: {InteropAlsa.StrError(rc)}");

                rc = InteropAlsa.snd_mixer_selem_register(mixer, IntPtr.Zero, IntPtr.Zero);
                if (rc < 0) throw new InvalidOperationException($"snd_mixer_selem_register failed: {InteropAlsa.StrError(rc)}");

                rc = InteropAlsa.snd_mixer_load(mixer);
                if (rc < 0) throw new InvalidOperationException($"snd_mixer_load failed: {InteropAlsa.StrError(rc)}");

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
                        int hasPlayback = InteropAlsa.snd_mixer_selem_has_playback_channel(elem, ch);
                        if (hasPlayback <= 0) continue;

                        // Make sure the element actually supports playback volume operations.
                        int hasVolume = InteropAlsa.snd_mixer_selem_has_playback_volume(elem);
                        if (hasVolume <= 0)
                        {
                            // Element reports playback channel but not volume; skip.
                            _log.Warn($"[ALSA] control='{controlName}' has playback channel but no playback volume support; skipping channel={ch}.");
                            continue;
                        }

                        unsafe
                        {
                            nint raw = 0;
                            nint min = 0;
                            nint max = 0;

                            rc = InteropAlsa.snd_mixer_selem_get_playback_volume(elem, ch, &raw);
                            if (rc < 0)
                            {
                                _log.Warn($"[ALSA] snd_mixer_selem_get_playback_volume failed for control='{controlName}' channel={ch}: {InteropAlsa.StrError(rc)}");
                                continue;
                            }

                            // get range
                            rc = InteropAlsa.snd_mixer_selem_get_playback_volume_range(elem, &min, &max);
                            if (rc < 0)
                            {
                                _log.Warn($"[ALSA] snd_mixer_selem_get_playback_volume_range failed for control='{controlName}' channel={ch}: {InteropAlsa.StrError(rc)}");
                                continue;
                            }

                            channels.Add(new MixerControlChannelInfo(ch.ToString(), raw, min, max, null, null));
                        }
                    }

                    list.Add(new MixerControlInfo(controlName, channels.ToArray()));

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

            return list.ToArray();
        }
    }
}
