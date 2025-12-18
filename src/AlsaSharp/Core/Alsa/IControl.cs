namespace AlsaSharp.Core.Alsa;

/// <summary>
/// Represents a control interface for ALSA mixer controls on a specific card.
/// </summary>
public interface IControl
{
    /// <summary>
    /// Returns the names of control elements available on the card.
    /// </summary>
    IEnumerable<string> GetControlElementNames();

    /// <summary>
    /// Gets the integer value of the named control element.
    /// </summary>
    /// <param name="elementName">The control element name.</param>
    /// <returns>The current value of the element.</returns>
    int GetControlElementValue(string elementName);

    /// <summary>
    /// Sets the integer value of the named control element.
    /// </summary>
    /// <param name="elementName">The control element name.</param>
    /// <param name="value">The value to set.</param>
    void SetControlElementValue(string elementName, int value);
}
