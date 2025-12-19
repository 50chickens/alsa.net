namespace AlsaSharp;

/// <summary>
/// settings on how to connect to and use your alsa devices
/// </summary>
public class SoundDeviceSettings()
{
    /// <summary>
    /// name of the playback device to use. Default: "default"
    /// </summary>
    public string PlaybackDeviceName { get; set; } = "default";

    /// <summary>
    /// name of the recording device to use. Default: "default"
    /// </summary>
    public string RecordingDeviceName { get; set; } = "default";

    /// <summary>
    /// name of the mixer device to use. Default: "default"
    /// </summary>
    public string MixerDeviceName { get; set; } = "default";

    /// <summary>
    /// sample rate to use for recording. Default: 48000
    /// </summary>
    /// <remarks>check your device specification for supported rates</remarks>
    public uint RecordingSampleRate { get; set; } = 48000;

    /// <summary>
    /// number of chanels to use for recording. Default: 2
    /// </summary>
    /// <remarks>check your device specification for available numbers</remarks>
    public ushort RecordingChannels { get; set; } = 2;

    /// <summary>
    /// number of bits per sample to use for recording. Default: 16
    /// </summary>
    /// <remarks>check device specification for supported bit depths</remarks>
    public ushort RecordingBitsPerSample { get; set; } = 16;

    /// <summary>
    /// List of supported sample rates (Hz) discovered by probing the device.
    /// </summary>
    public List<uint> SupportedSampleRates { get; set; } = new List<uint>();

    /// <summary>
    /// List of supported sample bits (bit depth) discovered by probing the device.
    /// </summary>
    public List<ushort> SupportedSampleBits { get; set; } = new List<ushort>();

    /// <summary>
    /// List of supported channel counts discovered by probing the device.
    /// </summary>
    public List<ushort> SupportedChannels { get; set; } = new List<ushort>();

    /// <summary>
    /// Supported format combinations (rate, bits, channels) discovered by probing.
    /// </summary>
    public List<(uint Rate, ushort Bits, ushort Channels)> SupportedCombinations { get; set; } = new List<(uint, ushort, ushort)>();

    /// <summary>
    /// Card identifier (eg. "Plus").
    /// </summary>
    public string? CardId { get; set; }

    /// <summary>
    /// Card name (human friendly, eg. "JAM Plus").
    /// </summary>
    public string? CardName { get; set; }

    /// <summary>
    /// Card long descriptive name.
    /// </summary>
    public string? CardLongName { get; set; }

    /// <summary>
    /// Card index (hw:{index}).
    /// </summary>
    public int? CardIndex { get; set; }

    /// <summary>
    /// Path to the baseline JSON file that was (or will be) created for this device.
    /// When set by callers/builders this can be used by workers to append measurement entries.
    /// </summary>
    public string? BaselineFilePath { get; set; }
}
