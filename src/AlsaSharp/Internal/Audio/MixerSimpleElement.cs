namespace AlsaSharp.Internal.Audio;

/// <summary>
/// Represents information about a single mixer channel (volume, range and switch state).
/// </summary>
public class MixerSimpleElement(string name, nint raw, nint min, nint max, long? db, int? sw)
{
    /// <summary>Channel name.</summary>
    private readonly string _name = name ?? throw new ArgumentNullException("Name cannot be null");
    /// <summary>Raw volume value.</summary>
    private readonly nint _raw = raw;
    /// <summary>Minimum volume value.</summary>
    private readonly nint _min = min;
    /// <summary>Maximum volume value.</summary>
    private readonly nint _max = max;
    /// <summary>dB value if available.</summary>
    private readonly long? _db = db;
    /// <summary>Switch state if available.</summary>
    private readonly int? _sw = sw;
    public string Name { get => _name; init => _name = value ?? throw new ArgumentNullException("Name cannot be null"); }
    public nint Raw { get => _raw; init => _raw = value; }
    public nint Min { get => _min; init => _min = value; }
    public nint Max { get => _max; init => _max = value; }
    public long? Db { get => _db; init => _db = value; }
    public int? Switch { get => _sw; init => _sw = value; }
}
