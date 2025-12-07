// Hint Generation Process
// For each interface type (pcm, rawmidi, etc.), the system collects available devices
// For each device, it generates a human-readable name and description
// User-defined hints from configuration files are added to the list
// The complete list is returned to the application
// Device names typically include card name, device identifier, and sometimes stream direction information.

// \brief Get a set of device name hints
//  * \param card Card number or -1 (means all cards)
//  * \param iface Interface identification (like "pcm", "rawmidi", "timer", "seq")
//  * \param hints Result - array of device name hints
//  * \result zero if success, otherwise a negative error code
//  *
//  * hints will receive a NULL-terminated array of device name hints,
//  * which can be passed to #snd_device_name_get_hint to extract usable
//  * values. When no longer needed, hints should be passed to
//  * #snd_device_name_free_hint to release resources.


//create _snd_ctl_elem_iface enum for the interface identification parameter


using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AlsaSharp.Internal;
using AlsaSharp.Internal.Audio;


public enum InterfaceIdentificationType
{
    SND_CTL_ELEM_IFACE_CARD = 0,
    SND_CTL_ELEM_IFACE_HWDEP = 1,
    SND_CTL_ELEM_IFACE_MIXER = 2,
    SND_CTL_ELEM_IFACE_PCM = 3,
    SND_CTL_ELEM_IFACE_RAWMIDI = 4,
    SND_CTL_ELEM_IFACE_TIMER = 5,
    SND_CTL_ELEM_IFACE_SEQUENCER = 6,
    SND_CTL_ELEM_IFACE_LAST = 7
}

public class AlsaHint
{
    public AlsaHint(string name, string? desc, string? ioid, string cardId, int cardIndex, int deviceIndex, InterfaceIdentificationType interfaceType, Control controlInterface, string? longName)
    {
        Name = name;
        if (desc == null) throw new InvalidOperationException("AlsaHint: DESC is null");
        Description = desc;
        IOID = ioid;
        CardId = cardId;
        CardIndex = cardIndex;
        DeviceIndex = deviceIndex;
        InterfaceType = interfaceType;
        ControlInterface = controlInterface;
        if (longName == null) throw new InvalidOperationException("AlsaHint: LongName is null");
        LongName = longName;
    }

//     struct hint_list {
// 	char **list;
// 	unsigned int count;
// 	unsigned int allocated;
// 	const char *siface;
// 	snd_ctl_elem_iface_t iface;
// 	snd_ctl_t *ctl;
// 	snd_ctl_card_info_t *info;
// 	int card;
// 	int device;
// 	long device_input;
// 	long device_output;
// 	int stream;
// 	int show_all;
// 	char *cardname;
// };
    
    public string Name { get; }
    public string Description { get; }
    public string? IOID { get; }
    public string CardId { get; }
    public int CardIndex { get; }
    public int DeviceIndex { get; }
    public InterfaceIdentificationType InterfaceType { get; }
    public Control ControlInterface { get; }
    public string LongName { get; }
    public string CardName => CardId;

    // No ToString formatting — keep the model a pure data model for the console app to format
    
}
public class AlsaCardInfo
{
    public AlsaCardInfo(int index, string? id, string? name, string? longName, string? driver, string? mixerName, string? components)
    {
        Index = index;
        if (id == null) throw new InvalidOperationException("AlsaCardInfo: id is null");
        Id = id;
        if (name == null) throw new InvalidOperationException("AlsaCardInfo: name is null");
        Name = name;
        if (longName == null) throw new InvalidOperationException("AlsaCardInfo: longName is null");
        LongName = longName;
        if (driver == null) throw new InvalidOperationException("AlsaCardInfo: driver is null");
        Driver = driver;
        if (mixerName == null) throw new InvalidOperationException("AlsaCardInfo: mixerName is null");
        MixerName = mixerName;
        if (components == null) throw new InvalidOperationException("AlsaCardInfo: components is null");
        Components = components;
        PcmEntries = new List<AlsaPcmEntry>();
    }
    public int Index { get; }
    public string Id { get; }
    public string Name { get; }
    public string LongName { get; }
    public string Driver { get; }
    public string MixerName { get; }
    public string Components { get; }
    public List<AlsaPcmEntry> PcmEntries { get; }

    // Keep this a simple model — no string formatting here. Worker/console app formats the output.
}

public class AlsaPcmEntry
{
    public AlsaPcmEntry(int deviceIndex, string id, string name, List<AlsaSubdevice> subdevices, int subdevicesCount, string stream)
    {
        DeviceIndex = deviceIndex;
        if (id == null) throw new InvalidOperationException("AlsaPcmEntry: id is null");
        Id = id;
        if (name == null) throw new InvalidOperationException("AlsaPcmEntry: name is null");
        Name = name;
        if (subdevices == null) throw new InvalidOperationException("AlsaPcmEntry: subdevices is null");
        Subdevices = subdevices;
        if (subdevices.Count == 0) throw new InvalidOperationException("AlsaPcmEntry: expected at least one subdevice");
        SubdeviceName = subdevices[0].Name;
        SubdevicesCount = subdevicesCount;
        if (stream == null) throw new InvalidOperationException("AlsaPcmEntry: stream is null");
        Stream = stream;
    }

    public int DeviceIndex { get; }
    public string Id { get; }
    public string Name { get; }
    public List<AlsaSubdevice> Subdevices { get; }
    public string SubdeviceName { get; }
    public int SubdevicesCount { get; }
    public string Stream { get; }
}

public class AlsaSubdevice
{
    public AlsaSubdevice(int subdeviceIndex, string? name)
    {
        SubdeviceIndex = subdeviceIndex;
        if (name == null) throw new InvalidOperationException("AlsaSubdevice: name is null");
        Name = name;
    }

    public int SubdeviceIndex { get; }
    public string Name { get; }
}
// ParsedName removed; parsing now uses TryParseName with out parameters.
public class AlsaHintService : IAlsaHintService
{
    private List<AlsaHint> _hints;
    private List<AlsaCardInfo> _cards;
    
    public AlsaHintService()
    {
        _hints = GetAlsaHints();
        _cards = GetAlsaCardInfos();
    }

    public List<AlsaHint> Hints 
    { 
        get 
        {
            return _hints;
        } 
    }
    public List<AlsaCardInfo> CardInfos
    {
        get => _cards;
    }
    private List<AlsaHint> GetAlsaHints()
    {
        var allHints = new List<AlsaHint>();
        allHints.AddRange(GetHintsForInterface("pcm"));
        allHints.AddRange(GetHintsForInterface("ctl"));
        return allHints;
    }

    private List<AlsaHint> GetHintsForInterface(string iface)
    {
        List<AlsaHint> hints = new List<AlsaHint>();
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
        var s = Marshal.PtrToStringUTF8(valuePtr);
        // assumption: ALSA uses allocated strings that need free
        InteropAlsa.free(valuePtr);
        return s;
    }

    private AlsaHint? BuildHintFromPointer(IntPtr hintPtr)
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
        return new AlsaHint(name, desc, ioid, cardName, cardIndex, deviceIndex, iface, control, longName);
    }

    private static bool IsNameValid(string? name) => !string.IsNullOrWhiteSpace(name) && !name.Equals("null", StringComparison.OrdinalIgnoreCase);
    private bool IsHardwareDevice(string? name) => !string.IsNullOrWhiteSpace(name) && Regex.IsMatch(name, @"^hw:", RegexOptions.IgnoreCase); //use name ?? false || Regex.IsMatch(name, @"^hw:", RegexOptions.IgnoreCase);

    private bool TryParseName(string name, out InterfaceIdentificationType iface, out string cardId, out int cardIndex, out int deviceIndex)
    {
        iface = InterfaceIdentificationType.SND_CTL_ELEM_IFACE_LAST;
        cardId = string.Empty;
        cardIndex = -1;
        deviceIndex = -1;
        if (TryParseCardPattern(name, out iface, out cardId, out cardIndex, out deviceIndex)) return true;
        if (TryParseIndexPattern(name, out iface, out cardId, out cardIndex, out deviceIndex)) return true;
        return false;
    }

    private List<AlsaCardInfo> GetAlsaCardInfos()
    {
        var results = new List<AlsaCardInfo>();
            int card = -1;
            InteropAlsa.snd_card_next(ref card);
            while (card >= 0)
            {
                string? id = null, name = null, longname = null, driver = null, mixer = null, components = null;
                IntPtr ctl = IntPtr.Zero;
                var ctlName = $"hw:{card}";
                if (InteropAlsa.snd_ctl_open(out ctl, ctlName, 0) == 0)
                {
                    if (InteropAlsa.snd_ctl_card_info_malloc(out var info) == 0)
                    {
                        if (InteropAlsa.snd_ctl_card_info(ctl, info) == 0)
                        {
                            id = Marshal.PtrToStringUTF8(InteropAlsa.snd_ctl_card_info_get_id(info));
                            if (id == null) throw new InvalidOperationException("GetAlsaCardInfos: card id is null");
                            name = Marshal.PtrToStringUTF8(InteropAlsa.snd_ctl_card_info_get_name(info));
                            if (name == null) throw new InvalidOperationException("GetAlsaCardInfos: card name is null");
                            longname = Marshal.PtrToStringUTF8(InteropAlsa.snd_ctl_card_info_get_longname(info));
                            if (longname == null) throw new InvalidOperationException("GetAlsaCardInfos: card longname is null");
                            driver = Marshal.PtrToStringUTF8(InteropAlsa.snd_ctl_card_info_get_driver(info));
                            if (driver == null) throw new InvalidOperationException("GetAlsaCardInfos: card driver is null");
                            mixer = Marshal.PtrToStringUTF8(InteropAlsa.snd_ctl_card_info_get_mixername(info));
                            if (mixer == null) throw new InvalidOperationException("GetAlsaCardInfos: card mixer is null");
                            components = Marshal.PtrToStringUTF8(InteropAlsa.snd_ctl_card_info_get_components(info));
                            if (components == null) throw new InvalidOperationException("GetAlsaCardInfos: card components is null");
                        }
                        InteropAlsa.snd_ctl_card_info_free(info);
                    }

                    var cardInfo = new AlsaCardInfo(card, id, name, longname, driver, mixer, components);

                    
                    {
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
                                            var subdevices = new List<AlsaSubdevice>();
                                            for (var s = 0; s < subCount; ++s)
                                            {
                                                InteropAlsa.snd_pcm_info_set_subdevice(pcmInfo, s);
                                                InteropAlsa.snd_pcm_info_set_stream(pcmInfo, stream);
                                                if (InteropAlsa.snd_ctl_pcm_info(ctl, pcmInfo) == 0)
                                                {
                                                    var subname = Marshal.PtrToStringUTF8(InteropAlsa.snd_pcm_info_get_subdevice_name(pcmInfo));
                                                    if (subname == null) throw new InvalidOperationException("GetAlsaCardInfos: pcm subdevice name is null");
                                                    subdevices.Add(new AlsaSubdevice(s, subname));
                                                }
                                            }
                                            var entry = new AlsaPcmEntry(device, pid, pname, subdevices, subCount, stream.ToString());
                                            cardInfo.PcmEntries.Add(entry);
                                        }
                                    }
                                    InteropAlsa.snd_pcm_info_free(pcmInfo);
                                }
                                InteropAlsa.snd_ctl_pcm_next_device(ctl, ref device);
                            }
                        }
                    }

                    InteropAlsa.snd_ctl_close(ctl);
                    results.Add(cardInfo);
                }
                else
                {
                    // If we couldn't open the ctl handle, still add a stub entry with minimal data (avoid nulls)
                    results.Add(new AlsaCardInfo(card, $"card{card}", $"card{card}", $"card{card}", "unknown", "unknown", "unknown"));
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

    private static InterfaceIdentificationType MapInterfaceToType(string ifaceStr) //use an extension method that returns a InterfaceIdentificationType from the ifaceStr
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

public interface IAlsaHintService
{
    List<AlsaHint> Hints { get; }
    List<AlsaCardInfo> CardInfos { get; }
}

public static class AlsaHintServiceBuilder 
{
    public static IAlsaHintService Build() => new AlsaHintService();
}