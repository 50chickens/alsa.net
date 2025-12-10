using System;
using System.Text;
using AlsaSharp.Internal.Audio;

public class AlsaHint
{
    public AlsaHint(string name, string description, string ioid, string cardId, int cardIndex, int deviceIndex, InterfaceIdentificationType interfaceType, Control controlInterface, string longName)
    {
        Name = name;
        Description = description;
        IOID = ioid;
        CardId = cardId;
        CardIndex = cardIndex;
        DeviceIndex = deviceIndex;
        InterfaceType = interfaceType;
        ControlInterface = controlInterface;
        LongName = longName;
    }
    
    public string Name { get; }
    public string Description { get; }
    public string IOID { get; }
    public string CardId { get; }
    public int CardIndex { get; }
    public int DeviceIndex { get; }
    public InterfaceIdentificationType InterfaceType { get; }
    public Control ControlInterface { get; }
    public string LongName { get; }
    public string CardName => CardId;
    
}
