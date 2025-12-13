namespace AlsaSharp.Core.Alsa;

/// <summary>
/// The element data type used by a control element.
/// </summary>
public enum ControlElementType
{
    /// <summary>Integer value type.</summary>
    Integer,
    /// <summary>64-bit integer type.</summary>
    Integer64,
    /// <summary>Boolean type.</summary>
    Boolean,
    /// <summary>Enumerated type.</summary>
    Enumerated,
    /// <summary>Bytes type.</summary>
    Bytes,
    /// <summary>IEC958 encoded type.</summary>
    IEC958
}
