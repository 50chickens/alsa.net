using System.Collections.Generic;

/// <summary>
/// DTO describing a subdevice of a PCM device.
/// </summary>
public class SubdeviceDto(int subdeviceIndex, string name)
{
    /// <summary>
    /// Index of the subdevice.
    /// </summary>
    private readonly int _subdeviceIndex = subdeviceIndex;
    /// <summary>
    /// Name of the subdevice.
    /// </summary>
    private readonly string _name = name ?? throw new ArgumentNullException("Name cannot be null");
    /// <summary>
    /// Gets the index of the subdevice.
    /// </summary>
    public int SubdeviceIndex { get => _subdeviceIndex; init => _subdeviceIndex = value; }
    /// <summary>
    /// Gets the name of the subdevice.
    /// </summary>
    public string Name { get => _name; init => _name = value ?? throw new ArgumentNullException("Name cannot be null"); }
}

/// <summary>
/// DTO describing a PCM device and its subdevices.
/// </summary>
public class PcmDeviceDto(int deviceIndex, string id, string name, List<SubdeviceDto> subdevices)
{
    /// <summary>
    /// Device index.
    /// </summary>
    private readonly int _deviceIndex = deviceIndex;
    /// <summary>
    /// Device id string.
    /// </summary>
    private readonly string _id = id ?? throw new ArgumentNullException("Id cannot be null");
    /// <summary>
    /// Device name.
    /// </summary>
    private readonly string _name = name ?? throw new ArgumentNullException("Name cannot be null");
    /// <summary>
    /// List of subdevices.
    /// </summary>
    private readonly List<SubdeviceDto> _subdevices = subdevices ?? new List<SubdeviceDto>();
    /// <summary>
    /// Gets the device index.
    /// </summary>
    public int DeviceIndex { get => _deviceIndex; init => _deviceIndex = value; }
    /// <summary>
    /// Gets the device id string.
    /// </summary>
    public string Id { get => _id; init => _id = value ?? throw new ArgumentNullException("Id cannot be null"); }
    /// <summary>
    /// Gets the device name.
    /// </summary>
    public string Name { get => _name; init => _name = value ?? throw new ArgumentNullException("Name cannot be null"); }
    /// <summary>
    /// Gets the list of subdevices.
    /// </summary>
    public List<SubdeviceDto> Subdevices { get => _subdevices; init => _subdevices = value ?? new List<SubdeviceDto>(); }
}

/// <summary>
/// DTO grouping PCM devices by stream type (playback/capture).
/// </summary>
public class PcmStreamDto(string stream, List<PcmDeviceDto> devices)
{
    /// <summary>
    /// Stream type identifier.
    /// </summary>
    private readonly string _stream = stream ?? throw new ArgumentNullException("Stream cannot be null");
    /// <summary>
    /// Devices in this stream.
    /// </summary>
    private readonly List<PcmDeviceDto> _devices = devices ?? new List<PcmDeviceDto>();
    /// <summary>
    /// Gets the stream type identifier.
    /// </summary>
    public string Stream { get => _stream; init => _stream = value ?? throw new ArgumentNullException("Stream cannot be null"); }
    /// <summary>
    /// Gets the devices in this stream.
    /// </summary>
    public List<PcmDeviceDto> Devices { get => _devices; init => _devices = value ?? new List<PcmDeviceDto>(); }
}

/// <summary>
/// DTO describing an ALSA card in the canonical alsactl output shape.
/// </summary>
public class CtlCardDto(int cardIndex, string id, string name, string longName, string driverName, string mixerName, string components, int controlsCount, List<string> controls, List<PcmStreamDto> pcm)
{
    /// <summary>
    /// Card index.
    /// </summary>
    private readonly int _cardIndex = cardIndex;
    /// <summary>
    /// Card id.
    /// </summary>
    private readonly string _id = id ?? throw new ArgumentNullException("Id cannot be null");
    /// <summary>
    /// Card name.
    /// </summary>
    private readonly string _name = name ?? throw new ArgumentNullException("Name cannot be null");
    /// <summary>
    /// Long card name.
    /// </summary>
    private readonly string _longName = longName ?? throw new ArgumentNullException("LongName cannot be null");
    /// <summary>
    /// Driver name.
    /// </summary>
    private readonly string _driverName = driverName ?? throw new ArgumentNullException("DriverName cannot be null");
    /// <summary>
    /// Mixer name.
    /// </summary>
    private readonly string _mixerName = mixerName ?? throw new ArgumentNullException("MixerName cannot be null");
    /// <summary>
    /// Components string.
    /// </summary>
    private readonly string _components = components ?? throw new ArgumentNullException("Components cannot be null");
    /// <summary>
    /// Number of control elements.
    /// </summary>
    private readonly int _controlsCount = controlsCount;
    /// <summary>
    /// Control element names.
    /// </summary>
    private readonly List<string> _controls = controls ?? new List<string>();
    /// <summary>
    /// PCM streams grouped by stream type.
    /// </summary>
    private readonly List<PcmStreamDto> _pcm = pcm ?? new List<PcmStreamDto>();
    /// <summary>
    /// Gets the card index.
    /// </summary>
    public int CardIndex { get => _cardIndex; init => _cardIndex = value; }
    /// <summary>
    /// Gets the card id.
    /// </summary>
    public string Id { get => _id; init => _id = value ?? throw new ArgumentNullException("Id cannot be null"); }
    /// <summary>
    /// Gets the card name.
    /// </summary>
    public string Name { get => _name; init => _name = value ?? throw new ArgumentNullException("Name cannot be null"); }
    /// <summary>
    /// Gets the long card name.
    /// </summary>
    public string LongName { get => _longName; init => _longName = value ?? throw new ArgumentNullException("LongName cannot be null"); }
    /// <summary>
    /// Gets the driver name.
    /// </summary>
    public string DriverName { get => _driverName; init => _driverName = value ?? throw new ArgumentNullException("DriverName cannot be null"); }
    /// <summary>
    /// Gets the mixer name.
    /// </summary>
    public string MixerName { get => _mixerName; init => _mixerName = value ?? throw new ArgumentNullException("MixerName cannot be null"); }
    /// <summary>
    /// Gets the components string.
    /// </summary>
    public string Components { get => _components; init => _components = value ?? throw new ArgumentNullException("Components cannot be null"); }
    /// <summary>
    /// Gets the number of control elements.
    /// </summary>
    public int ControlsCount { get => _controlsCount; init => _controlsCount = value; }
    /// <summary>
    /// Gets the control element names.
    /// </summary>
    public List<string> Controls { get => _controls; init => _controls = value ?? new List<string>(); }
    /// <summary>
    /// Gets the PCM streams grouped by stream type.
    /// </summary>
    public List<PcmStreamDto> Pcm { get => _pcm; init => _pcm = value ?? new List<PcmStreamDto>(); }
}

/// <summary>
/// Canonical hint DTO used by the comparison tooling.
/// </summary>
public class HintDto(string name, string cardId, int cardIndex, int? deviceIndex, string description, string longName, string ioid, string interfaceType, int controlCardIndex)
{
    /// <summary>
    /// Raw hint name.
    /// </summary>
    private readonly string _name = name ?? throw new ArgumentNullException("Name cannot be null");
    /// <summary>
    /// Associated card id.
    /// </summary>
    private readonly string _cardId = cardId ?? throw new ArgumentNullException("CardId cannot be null");
    /// <summary>
    /// Card index.
    /// </summary>
    private readonly int _cardIndex = cardIndex;
    /// <summary>
    /// Device index if applicable.
    /// </summary>
    private readonly int? _deviceIndex = deviceIndex;
    /// <summary>
    /// Description text.
    /// </summary>
    private readonly string _description = description ?? throw new ArgumentNullException("Description cannot be null");
    /// <summary>
    /// Long descriptive name.
    /// </summary>
    private readonly string _longName = longName ?? throw new ArgumentNullException("LongName cannot be null");
    /// <summary>
    /// IOID field from the hint.
    /// </summary>
    private readonly string _ioid = ioid ?? throw new ArgumentNullException("IOID cannot be null");
    /// <summary>
    /// Interface type string (e.g. "SND_CTL_ELEM_IFACE_PCM").
    /// </summary>
    private readonly string _interfaceType = interfaceType ?? throw new ArgumentNullException("InterfaceType cannot be null");
    /// <summary>
    /// Card index used for control operations.
    /// </summary>
    private readonly int _controlCardIndex = controlCardIndex;
    /// <summary>
    /// Gets the raw hint name.
    /// </summary>
    public string Name { get => _name; init => _name = value ?? throw new ArgumentNullException("Name cannot be null"); }
    /// <summary>
    /// Gets the associated card id.
    /// </summary>
    public string CardId { get => _cardId; init => _cardId = value ?? throw new ArgumentNullException("CardId cannot be null"); }
    /// <summary>
    /// Gets the card index.
    /// </summary>
    public int CardIndex { get => _cardIndex; init => _cardIndex = value; }
    /// <summary>
    /// Gets the device index if applicable.
    /// </summary>
    public int? DeviceIndex { get => _deviceIndex; init => _deviceIndex = value; }
    /// <summary>
    /// Gets the description text.
    /// </summary>
    public string Description { get => _description; init => _description = value ?? throw new ArgumentNullException("Description cannot be null"); }
    /// <summary>
    /// Gets the long descriptive name.
    /// </summary>
    public string LongName { get => _longName; init => _longName = value ?? throw new ArgumentNullException("LongName cannot be null"); }
    /// <summary>
    /// Gets the IOID field from the hint.
    /// </summary>
    public string IOID { get => _ioid; init => _ioid = value ?? throw new ArgumentNullException("IOID cannot be null"); }
    /// <summary>
    /// Gets the interface type string (e.g. "SND_CTL_ELEM_IFACE_PCM").
    /// </summary>
    public string InterfaceType { get => _interfaceType; init => _interfaceType = value ?? throw new ArgumentNullException("InterfaceType cannot be null"); }
    /// <summary>
    /// Gets the card index used for control operations.
    /// </summary>
    public int ControlCardIndex { get => _controlCardIndex; init => _controlCardIndex = value; }
}
