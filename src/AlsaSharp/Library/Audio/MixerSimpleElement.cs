namespace AlsaSharp.Library.Audio;

/// <summary>
/// Represents information about a single mixer channel (volume, range and switch state).
/// </summary>
public class MixerSimpleElement(string name, nint raw, nint min, nint max, long? db, int? sw)
{
    /// <summary>
    /// Channel name.
    /// </summary>
    private readonly string _name = name ?? throw new ArgumentNullException("Name cannot be null");
    /// <summary>
    /// Raw volume value.
    /// </summary>
    private readonly nint _raw = raw;
    /// <summary>
    /// Minimum volume value.
    /// </summary>
    private readonly nint _min = min;
    /// <summary>
    /// Maximum volume value.
    /// </summary>
    private readonly nint _max = max;
    /// <summary>
    /// dB value if available.
    /// </summary>
    private readonly long? _db = db;
    /// <summary>
    /// Switch state if available.
    /// </summary>
    private readonly int? _sw = sw;
    /// <summary>
    /// Gets the channel name.
    /// </summary>
    public string Name { get => _name; init => _name = value ?? throw new ArgumentNullException("Name cannot be null"); }
    /// <summary>
    /// Gets the raw/native value for the channel volume.
    /// </summary>
    public nint Raw { get => _raw; init => _raw = value; }
    /// <summary>
    /// Gets the minimum supported volume for the channel.
    /// </summary>
    public nint Min { get => _min; init => _min = value; }
    /// <summary>
    /// Gets the maximum supported volume for the channel.
    /// </summary>
    public nint Max { get => _max; init => _max = value; }
    /// <summary>
    /// Gets the dB value for the channel if available.
    /// </summary>
    public long? Db { get => _db; init => _db = value; }
    /// <summary>
    /// Gets the switch state (on/off) for the channel if available.
    /// </summary>
    public int? Switch { get => _sw; init => _sw = value; }
}
