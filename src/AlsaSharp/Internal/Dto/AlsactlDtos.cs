using System.Collections.Generic;

/// <summary>
/// DTO describing a subdevice of a PCM device.
/// </summary>
public class SubdeviceDto
{
    /// <summary>Index of the subdevice.</summary>
    public int SubdeviceIndex { get; set; }
    /// <summary>Name of the subdevice.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Parameterless constructor for serializer.</summary>
    public SubdeviceDto() { }
    /// <summary>Initializes a new instance of <see cref="SubdeviceDto"/>.</summary>
    public SubdeviceDto(int SubdeviceIndex, string Name)
    {
        this.SubdeviceIndex = SubdeviceIndex;
        this.Name = Name;
    }
}

/// <summary>
/// DTO describing a PCM device and its subdevices.
/// </summary>
public class PcmDeviceDto
{
    /// <summary>Device index.</summary>
    public int DeviceIndex { get; set; }
    /// <summary>Device id string.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Device name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>List of subdevices.</summary>
    public List<SubdeviceDto> Subdevices { get; set; } = new List<SubdeviceDto>();

    /// <summary>Parameterless constructor for serializer.</summary>
    public PcmDeviceDto() { }
    /// <summary>Initializes a new instance of <see cref="PcmDeviceDto"/>.</summary>
    public PcmDeviceDto(int DeviceIndex, string Id, string Name, List<SubdeviceDto> Subdevices)
    {
        this.DeviceIndex = DeviceIndex;
        this.Id = Id;
        this.Name = Name;
        this.Subdevices = Subdevices ?? new List<SubdeviceDto>();
    }
}

/// <summary>
/// DTO grouping PCM devices by stream type (playback/capture).
/// </summary>
public class PcmStreamDto
{
    /// <summary>Stream type identifier.</summary>
    public string Stream { get; set; } = string.Empty;
    /// <summary>Devices in this stream.</summary>
    public List<PcmDeviceDto> Devices { get; set; } = new List<PcmDeviceDto>();

    /// <summary>Parameterless constructor for serializer.</summary>
    public PcmStreamDto() { }
    /// <summary>Initializes a new instance of <see cref="PcmStreamDto"/>.</summary>
    public PcmStreamDto(string Stream, List<PcmDeviceDto> Devices)
    {
        this.Stream = Stream;
        this.Devices = Devices ?? new List<PcmDeviceDto>();
    }
}

/// <summary>
/// DTO describing an ALSA card in the canonical alsactl output shape.
/// </summary>
public class CtlCardDto
{
    /// <summary>Card index.</summary>
    public int CardIndex { get; set; }
    /// <summary>Card id.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Card name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Long card name.</summary>
    public string LongName { get; set; } = string.Empty;
    /// <summary>Driver name.</summary>
    public string DriverName { get; set; } = string.Empty;
    /// <summary>Mixer name.</summary>
    public string MixerName { get; set; } = string.Empty;
    /// <summary>Components string.</summary>
    public string Components { get; set; } = string.Empty;
    /// <summary>Number of control elements.</summary>
    public int ControlsCount { get; set; }
    /// <summary>Control element names.</summary>
    public List<string> Controls { get; set; } = new List<string>();
    /// <summary>PCM streams grouped by stream type.</summary>
    public List<PcmStreamDto> Pcm { get; set; } = new List<PcmStreamDto>();

    /// <summary>Parameterless constructor for serializer.</summary>
    public CtlCardDto() { }
    /// <summary>Initializes a new instance of <see cref="CtlCardDto"/>.</summary>
    public CtlCardDto(int CardIndex, string Id, string Name, string LongName, string DriverName, string MixerName, string Components, int ControlsCount, List<string> Controls, List<PcmStreamDto> Pcm)
    {
        this.CardIndex = CardIndex;
        this.Id = Id;
        this.Name = Name;
        this.LongName = LongName;
        this.DriverName = DriverName;
        this.MixerName = MixerName;
        this.Components = Components;
        this.ControlsCount = ControlsCount;
        this.Controls = Controls ?? new List<string>();
        this.Pcm = Pcm ?? new List<PcmStreamDto>();
    }
}

/// <summary>
/// Canonical hint DTO used by the comparison tooling.
/// </summary>
public class HintDto
{
    /// <summary>Raw hint name.</summary>
    public string Name { get; set; }
    /// <summary>Associated card id.</summary>
    public string CardId { get; set; }
    /// <summary>Card index.</summary>
    public int CardIndex { get; set; }
    /// <summary>Device index if applicable.</summary>
    public int? DeviceIndex { get; set; }
    /// <summary>Description text.</summary>
    public string Description { get; set; }
    /// <summary>Long descriptive name.</summary>
    public string LongName { get; set; }
    /// <summary>IOID field from the hint.</summary>
    public string IOID { get; set; }
    /// <summary>Interface type string (e.g. "SND_CTL_ELEM_IFACE_PCM").</summary>
    public string InterfaceType { get; set; }
    /// <summary>Card index used for control operations.</summary>
    public int ControlCardIndex { get; set; }

    
    /// <summary>Initializes a new instance of <see cref="HintDto"/>.</summary>
    public HintDto(string Name, string CardId, int CardIndex, int? DeviceIndex, string Description, string LongName, string IOID, string InterfaceType, int ControlCardIndex)
    {
        this.Name = Name;
        this.CardId = CardId;
        this.CardIndex = CardIndex;
        this.DeviceIndex = DeviceIndex;
        this.Description = Description;
        this.LongName = LongName;
        this.IOID = IOID;
        this.InterfaceType = InterfaceType;
        this.ControlCardIndex = ControlCardIndex;
    }
}
