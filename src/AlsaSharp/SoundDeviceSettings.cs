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
