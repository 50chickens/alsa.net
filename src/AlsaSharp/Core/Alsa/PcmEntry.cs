namespace AlsaSharp.Core.Alsa
{

    /// <summary>
    /// Represents a PCM device entry discovered on a card.
    /// </summary>
    public class PcmEntry
    {
        /// <summary>Backing field for DeviceIndex.</summary>
        private readonly int _deviceIndex;
        /// <summary>Backing field for Id.</summary>
        private readonly string _id;
        /// <summary>Backing field for Name.</summary>
        private readonly string _name;
        /// <summary>Backing field for Subdevices.</summary>
        private readonly List<Subdevice> _subdevices;
        /// <summary>Backing field for SubdeviceName.</summary>
        private readonly string _subdeviceName;
        /// <summary>Backing field for SubdevicesCount.</summary>
        private readonly int _subdevicesCount;
        /// <summary>Backing field for Stream.</summary>
        private readonly string _stream;

        /// <summary>Initializes a new instance of <see cref="PcmEntry"/>.</summary>
        public PcmEntry(int deviceIndex, string id, string name, List<Subdevice> subdevices, int subdevicesCount, string stream)
        {
            _deviceIndex = deviceIndex;
            _id = id ?? throw new InvalidOperationException("AlsaPcmEntry: id is null");
            _name = name ?? throw new InvalidOperationException("AlsaPcmEntry: name is null");
            _subdevices = subdevices ?? throw new InvalidOperationException("AlsaPcmEntry: subdevices is null");
            if (_subdevices.Count == 0) throw new InvalidOperationException("AlsaPcmEntry: expected at least one subdevice");
            _subdeviceName = _subdevices[0].Name;
            _subdevicesCount = subdevicesCount;
            _stream = stream ?? throw new InvalidOperationException("AlsaPcmEntry: stream is null");
        }

        /// <summary>Device index.</summary>
        public int DeviceIndex => _deviceIndex;
        /// <summary>Device id.</summary>
        public string Id => _id;
        /// <summary>Device name.</summary>
        public string Name => _name;
        /// <summary>List of subdevices.</summary>
        public List<Subdevice> Subdevices => _subdevices;
        /// <summary>Name of the primary subdevice.</summary>
        public string SubdeviceName => _subdeviceName;
        /// <summary>Number of subdevices.</summary>
        public int SubdevicesCount => _subdevicesCount;
        /// <summary>Stream type (e.g. "PLAYBACK"/"CAPTURE").</summary>
        public string Stream => _stream;
    }
}