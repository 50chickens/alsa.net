using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using AlsaSharp.Core.Alsa;
using AlsaSharp.Core.Native;
using Microsoft.Extensions.Logging;

namespace AlsaSharp.Library.Services
{
    /// <summary>
    /// Service that enumerates ALSA device hints and card information and provides
    /// canonical DTO mappings used by the comparison tooling.
    /// </summary>
    public class HintService : IHintService
    {
        private List<Hint> _hints;
        private List<CardInfo> _cards;
        private readonly ILogger<HintService>? _log;

        /// <summary>
        /// Creates a new AlsaHintService.
        /// </summary>
        public HintService(ILogger<HintService>? log)
        {
            _hints = GetAlsaHints();
            _cards = GetAlsaCardInfos();
            _log = log;
        }

        /// <summary>
        /// All discovered ALSA hints.
        /// </summary>
        public List<Hint> Hints
        {
            get
            {
                return _hints;
            }
        }
        /// <summary>
        /// All discovered ALSA card information.
        /// </summary>
        public List<CardInfo> CardInfos
        {
            get => _cards;
        }

        /// <summary>
        /// Returns canonical DTOs representing the parsed output of alsactl.
        /// </summary>
        /// <returns>List of <see cref="CtlCardDto"/> objects.</returns>
        public List<CtlCardDto> GetAlsactlCards()
        {
            var list = new List<CtlCardDto>();
            foreach (var c in _cards)
            {
                // build pcm streams
                var pcmStreams = new List<PcmStreamDto>();
                // group entries by Stream value
                var groups = c.PcmEntries.GroupBy(e => e.Stream ?? string.Empty);
                foreach (var g in groups)
                {
                    var devices = new List<PcmDeviceDto>();
                    foreach (var e in g)
                    {
                        var subs = e.Subdevices?.Select(s => new SubdeviceDto(s.SubdeviceIndex, s.Name)).ToList() ?? new List<SubdeviceDto>();
                        devices.Add(new PcmDeviceDto(e.DeviceIndex, e.Id, e.Name, subs));
                    }
                    pcmStreams.Add(new PcmStreamDto(g.Key, devices));
                }

                var controlsCount = GetControlsCountSafe(c.Index);
                var controls = GetControlElementNamesSafe(c.Index);
                list.Add(new CtlCardDto(
                    c.Index,
                    c.Id,
                    c.Name,
                    c.LongName,
                    c.Driver,
                    c.MixerName,
                    c.Components,
                    controlsCount,
                    controls,
                    pcmStreams
                ));
            }
            return list;
        }

        private int GetControlsCountSafe(int cardIndex)
        {
            try
            {
                var ctlName = $"hw:{cardIndex}";
                if (InteropAlsa.snd_ctl_open(out var ctl, ctlName, 0) != 0) return 0;
                try
                {
                    if (InteropAlsa.snd_ctl_elem_list_malloc(out var list) != 0) return 0;
                    try
                    {
                        if (InteropAlsa.snd_ctl_elem_list(ctl, list) != 0) return 0;
                        var cnt = InteropAlsa.snd_ctl_elem_list_get_count(list);
                        return cnt;
                    }
                    finally
                    {
                        InteropAlsa.snd_ctl_elem_list_free(list);
                    }
                }
                finally
                {
                    InteropAlsa.snd_ctl_close(ctl);
                }
            }
            catch (Exception ex)
            {
                _log?.LogDebug(ex, "GetControlsCountSafe failed for card {CardIndex}", cardIndex);
                return 0;
            }
        }

        private List<string> GetControlElementNamesSafe(int cardIndex)
        {
            var results = new List<string>();
            try
            {
                var ctlName = $"hw:{cardIndex}";
                if (InteropAlsa.snd_mixer_open(out var mixer, 0) != 0) return results;
                try
                {
                    if (InteropAlsa.snd_mixer_attach(mixer, ctlName) != 0) return results;
                    if (InteropAlsa.snd_mixer_load(mixer) != 0) return results;
                    var elem = InteropAlsa.snd_mixer_first_elem(mixer);
                    while (elem != IntPtr.Zero)
                    {
                        var namePtr = InteropAlsa.snd_mixer_selem_get_name(elem);
                        if (namePtr != IntPtr.Zero)
                        {
                            var name = Marshal.PtrToStringUTF8(namePtr);
                            if (!string.IsNullOrEmpty(name)) results.Add(name);
                        }
                        elem = InteropAlsa.snd_mixer_elem_next(elem);
                    }
                }
                finally
                {
                    InteropAlsa.snd_mixer_close(mixer);
                }
            }
            catch (Exception ex)
            {
                // log and return whatever we collected (possibly empty)
                _log?.LogDebug(ex, "GetControlElementNamesSafe failed for card {CardIndex}", cardIndex);
            }
            return results;
        }

        /// <summary>
        /// Returns canonical hint DTOs representing the device hints in a stable shape.
        /// </summary>
        /// <returns>List of <see cref="HintDto"/> objects.</returns>
        public List<HintDto> GetCanonicalHints()
        {
            var list = new List<HintDto>();
            foreach (var h in _hints)
            {
                list.Add(new HintDto(
                    h.Name,
                    h.CardId,
                    h.CardIndex,
                    h.DeviceIndex >= 0 ? h.DeviceIndex : (int?)null,
                    h.Description ?? string.Empty,
                    h.LongName ?? string.Empty,
                    h.IOID ?? string.Empty,
                    h.InterfaceType.ToString(),
                    h.CardIndex
                ));
            }
            return list;
        }
        private List<Hint> GetAlsaHints()
        {
            var allHints = new List<Hint>();
            allHints.AddRange(GetHintsForInterface("pcm"));
            allHints.AddRange(GetHintsForInterface("ctl"));
            return allHints;
        }

        private List<Hint> GetHintsForInterface(string iface)
        {
            List<Hint> hints = new List<Hint>();
            int card = -1;
            nint hintsPtr;
            int result = InteropAlsa.snd_device_name_hint(card, iface, out hintsPtr);
            if (result < 0)
                throw new Exception(Marshal.PtrToStringAnsi(InteropAlsa.snd_strerror(result)));

            try
            {
                foreach (var hintPtr in GetHintList(hintsPtr))
                {
                    var hint = BuildHintFromPointer(hintPtr);
                    if (hint != null)
                        hints.Add(hint);
                }
            }
            finally
            {
                InteropAlsa.snd_device_name_free_hint(hintsPtr);
            }

            return hints;
        }

        private static List<IntPtr> GetHintList(IntPtr hintsPtr)
        {
            var results = new List<IntPtr>();
            var current = hintsPtr;
            while (true)
            {
                var hint = Marshal.ReadIntPtr(current);
                if (hint == IntPtr.Zero) break;
                results.Add(hint);
                current += IntPtr.Size;
            }
            return results;
        }

        private static string? GetHintValue(IntPtr hint, string key)
        {
            IntPtr valuePtr = InteropAlsa.snd_device_name_get_hint(hint, key);
            if (valuePtr == IntPtr.Zero) return null;
            var hintValue = Marshal.PtrToStringUTF8(valuePtr);
            InteropAlsa.free(valuePtr);
            return hintValue;
        }

        private Hint? BuildHintFromPointer(IntPtr hintPtr)
        {
            var name = GetHintValue(hintPtr, "NAME");
            var desc = GetHintValue(hintPtr, "DESC");
            var ioid = GetHintValue(hintPtr, "IOID");

            if (!IsNameValid(name)) return null;
            if (!IsHardwareDevice(name)) return null;

            // parse NAME using regex groups
            if (string.IsNullOrWhiteSpace(name)) return null;
            if (!TryParseName(name, out var iface, out var cardName, out var cardIndex, out var deviceIndex)) return null;

            var control = new Control(cardIndex);
            string longName = GetCardLongName(cardIndex);
            return new Hint(name ?? string.Empty, desc ?? string.Empty, ioid ?? string.Empty, cardName, cardIndex, deviceIndex, iface, control, longName);
        }

        private static bool IsNameValid(string? name) => !string.IsNullOrWhiteSpace(name) && !name!.Equals("null", StringComparison.OrdinalIgnoreCase);
        private bool IsHardwareDevice(string? name) => !string.IsNullOrWhiteSpace(name) && Regex.IsMatch(name!, @"^hw:", RegexOptions.IgnoreCase);

        private bool TryParseName(string name, out InterfaceIdentificationType iface, out string cardId, out int cardIndex, out int deviceIndex) //use an extension method instead
        {
            if (TryParseCardPattern(name, out iface, out cardId, out cardIndex, out deviceIndex)) return true;
            if (TryParseIndexPattern(name, out iface, out cardId, out cardIndex, out deviceIndex)) return true;
            return false;
        }

        private List<CardInfo> GetAlsaCardInfos()
        {
            var results = new List<CardInfo>();
            int card = -1;
            InteropAlsa.snd_card_next(ref card);
            while (card >= 0)
            {
                string id = string.Empty, name = string.Empty, longname = string.Empty, driver = string.Empty, mixer = string.Empty, components = string.Empty;
                IntPtr ctl = IntPtr.Zero;
                var ctlName = $"hw:{card}";
                if (InteropAlsa.snd_ctl_open(out ctl, ctlName, 0) == 0)
                {
                    if (InteropAlsa.snd_ctl_card_info_malloc(out var info) == 0)
                    {
                        if (InteropAlsa.snd_ctl_card_info(ctl, info) == 0)
                        {
                            id = Marshal.PtrToStringUTF8(InteropAlsa.snd_ctl_card_info_get_id(info)) ?? throw new InvalidOperationException("GetAlsaCardInfos: card id is null");
                            name = Marshal.PtrToStringUTF8(InteropAlsa.snd_ctl_card_info_get_name(info)) ?? throw new InvalidOperationException("GetAlsaCardInfos: card name is null");
                            longname = Marshal.PtrToStringUTF8(InteropAlsa.snd_ctl_card_info_get_longname(info)) ?? throw new InvalidOperationException("GetAlsaCardInfos: card longname is null");
                            driver = Marshal.PtrToStringUTF8(InteropAlsa.snd_ctl_card_info_get_driver(info)) ?? throw new InvalidOperationException("GetAlsaCardInfos: card driver is null");
                            mixer = Marshal.PtrToStringUTF8(InteropAlsa.snd_ctl_card_info_get_mixername(info)) ?? throw new InvalidOperationException("GetAlsaCardInfos: card mixer is null");
                            components = Marshal.PtrToStringUTF8(InteropAlsa.snd_ctl_card_info_get_components(info)) ?? throw new InvalidOperationException("GetAlsaCardInfos: card components is null");
                        }
                        InteropAlsa.snd_ctl_card_info_free(info);
                    }

                    var cardInfo = new CardInfo(card, id, name, longname, driver, mixer, components);

                    int device = -1;
                    if (InteropAlsa.snd_ctl_pcm_next_device(ctl, ref device) >= 0)
                    {
                        while (device >= 0)
                        {
                            if (InteropAlsa.snd_pcm_info_malloc(out var pcmInfo) == 0)
                            {
                                foreach (snd_pcm_stream_t stream in new[] { snd_pcm_stream_t.SND_PCM_STREAM_PLAYBACK, snd_pcm_stream_t.SND_PCM_STREAM_CAPTURE })
                                {
                                    InteropAlsa.snd_pcm_info_set_device(pcmInfo, device);
                                    InteropAlsa.snd_pcm_info_set_subdevice(pcmInfo, 0);
                                    InteropAlsa.snd_pcm_info_set_stream(pcmInfo, stream);
                                    if (InteropAlsa.snd_ctl_pcm_info(ctl, pcmInfo) == 0)
                                    {
                                        var pid = Marshal.PtrToStringUTF8(InteropAlsa.snd_pcm_info_get_id(pcmInfo));
                                        if (pid == null) throw new InvalidOperationException("GetAlsaCardInfos: pcm id is null");
                                        var pname = Marshal.PtrToStringUTF8(InteropAlsa.snd_pcm_info_get_name(pcmInfo));
                                        if (pname == null) throw new InvalidOperationException("GetAlsaCardInfos: pcm name is null");
                                        var subCount = InteropAlsa.snd_pcm_info_get_subdevices_count(pcmInfo);
                                        var subdevices = new List<Subdevice>();
                                        for (var s = 0; s < subCount; ++s)
                                        {
                                            InteropAlsa.snd_pcm_info_set_subdevice(pcmInfo, s);
                                            InteropAlsa.snd_pcm_info_set_stream(pcmInfo, stream);
                                            if (InteropAlsa.snd_ctl_pcm_info(ctl, pcmInfo) == 0)
                                            {
                                                var subname = Marshal.PtrToStringUTF8(InteropAlsa.snd_pcm_info_get_subdevice_name(pcmInfo));
                                                if (subname == null) throw new InvalidOperationException("GetAlsaCardInfos: pcm subdevice name is null");
                                                subdevices.Add(new Subdevice(s, subname));
                                            }
                                        }
                                        var entry = new PcmEntry(device, pid, pname, subdevices, subCount, stream.ToString());
                                        cardInfo.PcmEntries.Add(entry);
                                    }
                                }
                                InteropAlsa.snd_pcm_info_free(pcmInfo);
                            }
                            InteropAlsa.snd_ctl_pcm_next_device(ctl, ref device);
                        }
                    }

                    InteropAlsa.snd_ctl_close(ctl);
                    results.Add(cardInfo);
                }
                else
                {
                    // If we couldn't open the ctl handle, still add a stub entry with minimal data (avoid nulls)
                    results.Add(new CardInfo(card, $"card{card}", $"card{card}", $"card{card}", "unknown", "unknown", "unknown"));
                }
                InteropAlsa.snd_card_next(ref card);
            }
            return results;
        }

        private bool TryParseCardPattern(string name, out InterfaceIdentificationType iface, out string cardId, out int cardIndex, out int deviceIndex)
        {
            iface = InterfaceIdentificationType.SND_CTL_ELEM_IFACE_LAST;
            cardId = string.Empty;
            cardIndex = -1;
            deviceIndex = -1;
            var regexCard = new Regex(@"^(?<iface>[^:]+):CARD=(?<card>[^,]+)(?:,DEV=(?<dev>\d+))?$", RegexOptions.IgnoreCase);
            var m = regexCard.Match(name);
            if (!m.Success) return false;
            var ifaceStr = m.Groups["iface"].Value;
            cardId = m.Groups["card"].Value;
            var devStr = m.Groups["dev"].Value;
            int dev = 0;
            if (!string.IsNullOrEmpty(devStr)) dev = int.Parse(devStr);
            var cardIdx = GetCardIndexByName(cardId);
            var iftype = MapInterfaceToType(ifaceStr);
            iface = iftype;
            cardIndex = cardIdx;
            deviceIndex = dev;
            return true;
        }

        private bool TryParseIndexPattern(string name, out InterfaceIdentificationType iface, out string cardId, out int cardIndex, out int deviceIndex)
        {
            iface = InterfaceIdentificationType.SND_CTL_ELEM_IFACE_LAST;
            cardId = string.Empty;
            cardIndex = -1;
            deviceIndex = -1;
            var regexIndex = new Regex(@"^(?<iface>[^:]+):(?<card>\d+),(?<dev>\d+)$", RegexOptions.IgnoreCase);
            var m2 = regexIndex.Match(name);
            if (!m2.Success) return false;
            var ifaceStr = m2.Groups["iface"].Value;
            var cardNum = int.Parse(m2.Groups["card"].Value);
            var devNum = int.Parse(m2.Groups["dev"].Value);
            cardId = GetCardName(cardNum);
            var iftype = MapInterfaceToType(ifaceStr);
            iface = iftype;
            cardIndex = cardNum;
            deviceIndex = devNum;
            return true;
        }

        private static InterfaceIdentificationType MapInterfaceToType(string ifaceStr)
        {
            return ifaceStr.ToLowerInvariant() switch
            {
                "hw" or "plughw" or "dmix" or "dsnoop" => InterfaceIdentificationType.SND_CTL_ELEM_IFACE_PCM,
                "ctl" => InterfaceIdentificationType.SND_CTL_ELEM_IFACE_CARD,
                _ => InterfaceIdentificationType.SND_CTL_ELEM_IFACE_LAST,
            };
        }

        private int GetCardIndexByName(string cardName)
        {
            int card = -1;
            InteropAlsa.snd_card_next(ref card);
            while (card >= 0)
            {
                if (InteropAlsa.snd_card_get_name(card, out IntPtr p) == 0 && p != IntPtr.Zero)
                {
                    var name = Marshal.PtrToStringUTF8(p);
                    InteropAlsa.free(p);
                    if (name != null && name.Equals(cardName, StringComparison.OrdinalIgnoreCase)) return card;
                }
                // If not exact match on card ID, try longname contains matching
                if (InteropAlsa.snd_card_get_longname(card, out IntPtr longp) == 0 && longp != IntPtr.Zero)
                {
                    var longname = Marshal.PtrToStringUTF8(longp);
                    InteropAlsa.free(longp);
                    if (longname != null && longname.IndexOf(cardName, StringComparison.OrdinalIgnoreCase) >= 0) return card;
                }
                InteropAlsa.snd_card_next(ref card);
            }
            return -1;
        }

        private string GetCardName(int index)
        {
            if (InteropAlsa.snd_card_get_name(index, out IntPtr p) == 0 && p != IntPtr.Zero)
            {
                var s = Marshal.PtrToStringUTF8(p);
                InteropAlsa.free(p);
                if (s == null) throw new InvalidOperationException($"Could not get card name for index {index} (null string)");
                return s;
            }
            throw new InvalidOperationException($"Could not get card name for index {index}");
        }

        private string GetCardLongName(int index)
        {
            if (InteropAlsa.snd_card_get_longname(index, out IntPtr p) == 0 && p != IntPtr.Zero)
            {
                var s = Marshal.PtrToStringUTF8(p);
                InteropAlsa.free(p);
                if (s == null) throw new InvalidOperationException($"Could not get longname for card index {index} (null string)");
                return s;
            }
            throw new InvalidOperationException($"Could not get longname for card index {index}");
        }
    }
}