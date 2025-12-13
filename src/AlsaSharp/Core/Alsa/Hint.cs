namespace AlsaSharp.Core.Alsa
{

    /// <summary>
    /// Represents a single device hint returned by the ALSA device hint APIs.
    /// </summary>
    public class Hint
    {
        /// <summary>Creates a new <see cref="Hint"/> instance.</summary>
        public Hint(string name, string description, string ioid, string cardId, int cardIndex, int deviceIndex, InterfaceIdentificationType interfaceType, Control controlInterface, string longName)
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

        /// <summary>The hint name string.</summary>
        public string Name { get; }
        /// <summary>Description text from the hint.</summary>
        public string Description { get; }
        /// <summary>IOID field if present.</summary>
        public string IOID { get; }
        /// <summary>Associated card id.</summary>
        public string CardId { get; }
        /// <summary>Card index.</summary>
        public int CardIndex { get; }
        /// <summary>Device index if known.</summary>
        public int DeviceIndex { get; }
        /// <summary>Interface type parsed from the hint name.</summary>
        public InterfaceIdentificationType InterfaceType { get; }
        /// <summary>Optional control interface for the hint.</summary>
        public Control ControlInterface { get; }
        /// <summary>Long descriptive name for the card/device.</summary>
        public string LongName { get; }
        /// <summary>Alias for <see cref="CardId"/> used by some outputs.</summary>
        public string CardName => CardId;

    }
}