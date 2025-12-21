using System.Runtime.InteropServices;
using AlsaSharp.Core.Native;
using AlsaSharp.Library.Audio;
using AlsaSharp.Library.Logging;

namespace AlsaSharp.Library.Services
{
    /// <summary>
    /// Represents an ALSA sound card.
    /// </summary>
    public class SoundDeviceManager : ISoundDeviceManager
    {
        private readonly ILog<ISoundDeviceManager> _log;

        /// <summary>
        /// Creates a new instance of the Card class.
        /// </summary>
        /// <param name="log">Logger instance scoped to this type.</param>
        /// <param name="index">Card numeric id.</param>
        /// <param name="name">Card short name.</param>
        public SoundDeviceManager(ILog<ISoundDeviceManager> log, int index, string name)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>
        /// Enumerate mixer controls for this card using libasound APIs.
        /// Returns an array of MixerControlInfo with basic channel raw values and ranges.
        /// </summary>
        public List<MixerSimpleElement> GetMixerSimpleElements(ISoundDevice soundDevice)
        {
            soundDevice = soundDevice ?? throw new ArgumentNullException(nameof(soundDevice));
            //int _index = soundDevice.Settings;
            string mixerDeviceName = soundDevice.Settings.MixerDeviceName;

            var list = new List<MixerSimpleElement>();
            IntPtr mixer = IntPtr.Zero;
            Exception? primaryEx = null;
            try
            {
                // open mixer
                int returnCode = InteropAlsa.snd_mixer_open(out mixer, 0);
                if (returnCode < 0)
                    throw new InvalidOperationException($"snd_mixer_open failed: {InteropAlsa.StrError(returnCode)}");

                // Prefer attaching mixer by card short name when available
                // (e.g. "hw:CARD=IQaudIOCODEC"). If the MixerDeviceName
                // already contains the full ALSA device spec (starts with
                // "hw:"), use it directly; otherwise, assume it is a card
                // short name and construct the attach string.
                string attachName = mixerDeviceName.StartsWith("hw:", StringComparison.OrdinalIgnoreCase)
                    ? mixerDeviceName
                    : $"hw:CARD={mixerDeviceName}";

                returnCode = InteropAlsa.snd_mixer_attach(mixer, attachName);
                if (returnCode < 0)
                    throw new InvalidOperationException($"snd_mixer_attach failed: {InteropAlsa.StrError(returnCode)}");

                returnCode = InteropAlsa.snd_mixer_selem_register(mixer, IntPtr.Zero, IntPtr.Zero);
                if (returnCode < 0)
                    throw new InvalidOperationException($"snd_mixer_selem_register failed: {InteropAlsa.StrError(returnCode)}");

                returnCode = InteropAlsa.snd_mixer_load(mixer);
                if (returnCode < 0)
                    throw new InvalidOperationException($"snd_mixer_load failed: {InteropAlsa.StrError(returnCode)}");

                var elem = InteropAlsa.snd_mixer_first_elem(mixer);
                while (elem != IntPtr.Zero)
                {
                    var namePtr = InteropAlsa.snd_mixer_selem_get_name(elem);
                    if (namePtr == IntPtr.Zero)
                        throw new InvalidOperationException("Mixer element name pointer is null");
                    var simpleElementName = Marshal.PtrToStringUTF8(namePtr);
                    if (simpleElementName == null)
                        throw new InvalidOperationException("Mixer element name is null");
                    var channels = new List<MixerSimpleElement>();

                    // check common channels (front left/right and mono)
                    var channelIds = new[] { snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_LEFT, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_RIGHT, snd_mixer_selem_channel_id.SND_MIXER_SCHN_MONO };
                    foreach (var channelid in channelIds)
                    {
                        int hasPlayback = InteropAlsa.snd_mixer_selem_has_playback_channel(elem, channelid);
                        if (hasPlayback <= 0)
                            continue;

                        // Make sure the element actually supports playback volume operations.
                        int hasVolume = InteropAlsa.snd_mixer_selem_has_playback_volume(elem);
                        if (hasVolume <= 0)
                        {
                            // Element reports playback channel but not volume; skip.
                            _log.Warn($"[ALSA] simpleElement='{simpleElementName}' has playback channel but no playback volume support; skipping channelId={channelid}.");
                            continue;
                        }

                        unsafe
                        {
                            nint raw = 0;
                            nint min = 0;
                            nint max = 0;

                            returnCode = InteropAlsa.snd_mixer_selem_get_playback_volume(elem, channelid, &raw);
                            if (returnCode < 0)
                            {
                                _log.Warn($"[ALSA] snd_mixer_selem_get_playback_volume failed for control='{simpleElementName}' channelId={channelid}: {InteropAlsa.StrError(returnCode)}");
                                continue;
                            }

                            // get range
                            returnCode = InteropAlsa.snd_mixer_selem_get_playback_volume_range(elem, &min, &max);
                            if (returnCode < 0)
                            {
                                _log.Warn($"[ALSA] snd_mixer_selem_get_playback_volume_range failed for control='{simpleElementName}' channelId={channelid}: {InteropAlsa.StrError(returnCode)}");
                                continue;
                            }

                            channels.Add(new MixerSimpleElement(simpleElementName, raw, min, max, null, null));
                        }
                    }

                    var simpleElement = channels.FirstOrDefault();
                    if (simpleElement != null)
                    {
                        list.Add(simpleElement);
                    }

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
                    int returnCode = InteropAlsa.snd_mixer_close(mixer);
                    if (returnCode < 0)
                    {
                        var closeEx = new InvalidOperationException($"snd_mixer_close failed: {InteropAlsa.StrError(returnCode)}");
                        if (primaryEx != null)
                            throw new AggregateException(primaryEx, closeEx);
                        else
                            throw closeEx;
                    }
                }
            }

            return list;
        }
    }
}
