using System;
using System.Collections.Generic;
using System.Linq;

namespace AlsaSharp.Core.Alsa
{

    /// <summary>
    /// Represents discovered information about an ALSA card and its PCM entries.
    /// </summary>
    public class CardInfo
    {
        /// <summary>Backing field for Index.</summary>
        private readonly int _index;
        /// <summary>Backing field for Id.</summary>
        private readonly string _id;
        /// <summary>Backing field for Name.</summary>
        private readonly string _name;
        /// <summary>Backing field for LongName.</summary>
        private readonly string _longName;
        /// <summary>Backing field for Driver.</summary>
        private readonly string _driver;
        /// <summary>Backing field for MixerName.</summary>
        private readonly string _mixerName;
        /// <summary>Backing field for Components.</summary>
        private readonly string _components;
        /// <summary>Backing field for PcmEntries.</summary>
        private readonly List<PcmEntry> _pcmEntries;

        /// <summary>Initializes a new instance of <see cref="CardInfo"/>.</summary>
        public CardInfo(int index, string id, string name, string longName, string driver, string mixerName, string components)
        {
            _index = index;
            _id = id ?? throw new InvalidOperationException("AlsaCardInfo: id is null");
            _name = name ?? throw new InvalidOperationException("AlsaCardInfo: name is null");
            _longName = longName ?? throw new InvalidOperationException("AlsaCardInfo: longName is null");
            _driver = driver ?? throw new InvalidOperationException("AlsaCardInfo: driver is null");
            _mixerName = mixerName ?? throw new InvalidOperationException("AlsaCardInfo: mixerName is null");
            _components = components ?? throw new InvalidOperationException("AlsaCardInfo: components is null");
            _pcmEntries = new List<PcmEntry>();
        }

        /// <summary>
        /// Index of the card (e.g. 0 for "hw:0").
        /// </summary>
        public int Index => _index;
        /// <summary>
        /// Card short id (e.g. "Intel" or "snd_rpi_audio").
        /// </summary>
        public string Id => _id;
        /// <summary>Card name.</summary>
        public string Name => _name;
        /// <summary>Long descriptive name for the card.</summary>
        public string LongName => _longName;
        /// <summary>Driver name used by the card.</summary>
        public string Driver => _driver;
        /// <summary>Mixer name associated with the card.</summary>
        public string MixerName => _mixerName;
        /// <summary>ALSA components string for the card.</summary>
        public string Components => _components;
        /// <summary>List of PCM entries discovered for this card.</summary>
        public List<PcmEntry> PcmEntries => _pcmEntries;
        // Convenience properties to mirror the output shape used by the comparison script
        /// <summary>Device indexes for discovered PCM entries.</summary>
        public List<int> DeviceIndexes => PcmEntries?.Select(e => e.DeviceIndex).ToList() ?? new List<int>();
        /// <summary>Device ids for discovered PCM entries.</summary>
        public List<string> DeviceIds => PcmEntries?.Select(e => e.Id).ToList() ?? new List<string>();
        /// <summary>Device names for discovered PCM entries.</summary>
        public List<string> DeviceNames => PcmEntries?.Select(e => e.Name).ToList() ?? new List<string>();
        /// <summary>Concatenated summary of device ids.</summary>
        public string DeviceSummary => string.Join("; ", DeviceIds.Where(id => !string.IsNullOrEmpty(id)));
    }
}
