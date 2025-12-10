namespace AlsaSharp.Internal.Audio
{

    /// <summary>
    /// Represents a single PCM subdevice.
    /// </summary>
    public class Subdevice
    {
        private readonly int _subdeviceIndex;
        private readonly string _name;

        /// <summary>Initializes a new instance of <see cref="Subdevice"/>.</summary>
        public Subdevice(int subdeviceIndex, string name)
        {
            _subdeviceIndex = subdeviceIndex;
            _name = name ?? throw new InvalidOperationException("AlsaSubdevice: name is null");
        }

        /// <summary>Index of the subdevice.</summary>
        public int SubdeviceIndex => _subdeviceIndex;
        /// <summary>Name of the subdevice.</summary>
        public string Name => _name;
    }
}