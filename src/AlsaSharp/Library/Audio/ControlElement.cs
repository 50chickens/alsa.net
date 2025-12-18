using AlsaSharp.Core.Alsa;

namespace AlsaSharp.Library.Audio;

/// <summary>
/// Basic implementation of <see cref="IControlElement"/>.
/// </summary>
public class ControlElement : IControlElement
{
    /// <summary>Element identifier.</summary>
    public int Id { get; private set; }
    /// <summary>Element name.</summary>
    public string Name { get; private set; }
    /// <summary>Element data type.</summary>
    public ControlElementType ElementType { get; private set; }

    /// <summary>Initializes a new instance of <see cref="ControlElement"/>.</summary>
    public ControlElement(int id, string name, ControlElementType elementType)
    {
        Id = id;
        Name = name;
        ElementType = elementType;
    }

    /// <summary>Gets the value at the specified index.</summary>
    public int GetValue(int index)
    {
        // get the value of the control element at the specified index
        throw new NotImplementedException();
    }

    /// <summary>Sets the value at the specified index.</summary>
    public void SetValue(int index, int value)
    {
        // Implement logic to set the value of the control element at the specified index
        throw new NotImplementedException();
    }
}
