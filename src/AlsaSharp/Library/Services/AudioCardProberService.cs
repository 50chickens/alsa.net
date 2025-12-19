using AlsaSharp.Core.Native;
using AlsaSharp.Library.Logging;

namespace AlsaSharp.Library.Services
{
    /// <summary>
    /// Probes ALSA hardware devices for supported formats, sample rates, and channel counts.
    /// </summary>
    public class AudioCardProberService(ILog<AudioCardProberService> log)
    {
        private readonly ILog<AudioCardProberService> _log = log;

        // Common sample rates to probe
        private static readonly uint[] CommonRates = [8000, 11025, 16000, 22050, 32000, 44100, 48000, 88200, 96000, 176400, 192000];
        private static readonly ushort[] CommonChannels = [1, 2, 4, 6, 8];

        private readonly (snd_pcm_format_t fmt, ushort bits)[] FormatCandidates = new[]
        {
            (snd_pcm_format_t.SND_PCM_FORMAT_U8, (ushort)8),
            (snd_pcm_format_t.SND_PCM_FORMAT_S16_LE, (ushort)16),
            (snd_pcm_format_t.SND_PCM_FORMAT_S24_LE, (ushort)24),
            (snd_pcm_format_t.SND_PCM_FORMAT_S24_3LE, (ushort)24),
            (snd_pcm_format_t.SND_PCM_FORMAT_S32_LE, (ushort)32),
            (snd_pcm_format_t.SND_PCM_FORMAT_FLOAT_LE, (ushort)32),
        };

        /// <summary>
        /// Probes the specified sound device settings for hardware capabilities.
        /// </summary>
        /// <param name="settings">The settings object to populate with probed capabilities.</param>
        /// <returns>The updated settings object.</returns>
        public SoundDeviceSettings Probe(SoundDeviceSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            var supportedRates = new HashSet<uint>();
            var supportedBits = new HashSet<ushort>();
            var supportedChannels = new HashSet<ushort>();
            var supportedCombinations = new HashSet<(uint, ushort, ushort)>();

            IntPtr pcm = IntPtr.Zero;
            try
            {
                int rv = InteropAlsa.snd_pcm_open(ref pcm, settings.RecordingDeviceName, snd_pcm_stream_t.SND_PCM_STREAM_CAPTURE, 0);
                if (rv < 0 || pcm == IntPtr.Zero)
                {
                    _log.Warn($"[DeviceProber] Failed to open device {settings.RecordingDeviceName}: {rv}");
                    return settings; // best-effort: return unchanged
                }

                IntPtr @params = IntPtr.Zero;
                try
                {
                    if (InteropAlsa.snd_pcm_hw_params_malloc(ref @params) < 0)
                        return settings;

                    // start with any params
                    InteropAlsa.snd_pcm_hw_params_any(pcm, @params);

                    // discover supported formats (map to sample bits)
                    foreach (var (fmt, bits) in FormatCandidates)
                    {
                        try
                        {
                            int tf = InteropAlsa.snd_pcm_hw_params_test_format(pcm, @params, fmt);
                            if (tf == 0)
                            {
                                supportedBits.Add(bits);
                            }
                        }
                        catch { }
                    }

                    // discover supported channels by testing common channel counts
                    foreach (var ch in CommonChannels)
                    {
                        try
                        {
                            InteropAlsa.snd_pcm_hw_params_any(pcm, @params);
                            InteropAlsa.snd_pcm_hw_params_set_access(pcm, @params, snd_pcm_access_t.SND_PCM_ACCESS_RW_INTERLEAVED);
                            int sch = InteropAlsa.snd_pcm_hw_params_set_channels(pcm, @params, ch);
                            if (sch == 0)
                                supportedChannels.Add(ch);
                        }
                        catch { }
                    }

                    // discover supported rates by testing a set of common rates
                    foreach (var r in CommonRates)
                    {
                        try
                        {
                            InteropAlsa.snd_pcm_hw_params_any(pcm, @params);
                            InteropAlsa.snd_pcm_hw_params_set_access(pcm, @params, snd_pcm_access_t.SND_PCM_ACCESS_RW_INTERLEAVED);
                            unsafe
                            {
                                uint rr = r;
                                int dir = 0;
                                int rrRv = InteropAlsa.snd_pcm_hw_params_set_rate_near(pcm, @params, &rr, &dir);
                                if (rrRv == 0)
                                {
                                    supportedRates.Add(r);
                                }
                            }
                        }
                        catch { }
                    }
                    _log.Debug($"[DeviceProber] Probed {settings.RecordingDeviceName}: bits={supportedBits.Count}, rates={supportedRates.Count}, channels={supportedChannels.Count}");
                }
                finally
                {
                    if (@params != IntPtr.Zero) InteropAlsa.snd_pcm_hw_params_free(@params);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"[DeviceProber] Exception probing {settings.RecordingDeviceName}");
            }
            finally
            {
                try { if (pcm != IntPtr.Zero) InteropAlsa.snd_pcm_close(pcm); } catch { }
            }

            settings.SupportedSampleBits = supportedBits.OrderBy(x => x).ToList();
            settings.SupportedSampleRates = supportedRates.OrderBy(x => x).ToList();
            settings.SupportedChannels = supportedChannels.OrderBy(x => x).ToList();
            settings.SupportedCombinations = supportedCombinations.OrderBy(t => t.Item1).ThenBy(t => t.Item2).ThenBy(t => t.Item3).Select(t => (t.Item1, t.Item2, t.Item3)).ToList();

            return settings;
        }
    }
}
