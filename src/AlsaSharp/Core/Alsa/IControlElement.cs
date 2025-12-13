using AlsaSharp.Library.Audio;

namespace AlsaSharp.Core.Alsa;

/// <summary>
/// Represents a single control element exposed by the mixer.
/// </summary>
public interface IControlElement
{
    /// <summary>Unique identifier for the element.</summary>
    int Id { get; }
    /// <summary>Human-readable name for the element.</summary>
    string Name { get; }
    /// <summary>Type of the element.</summary>
    ControlElementType ElementType { get; }
    /// <summary>Get the value at the given index for multi-value elements.</summary>
    int GetValue(int index);
    /// <summary>Set the value at the given index for multi-value elements.</summary>
    void SetValue(int index, int value);
}
