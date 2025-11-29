using System;

namespace Alsa.Net.Internal
{
    public class MixerControlChannelInfo
    {
        // Match libasound types exactly: native-sized integers for volume/range, C long for dB, int for switch.
        public MixerControlChannelInfo(string name, nint raw, nint min, nint max, long? db, int? sw)
        {
            Name = name; Raw = raw; Min = min; Max = max; Db = db; Switch = sw;
        }
        public string Name { get; }
        public nint Raw { get; }
        public nint Min { get; }
        public nint Max { get; }
        public long? Db { get; }
        public int? Switch { get; }
    }

    public class MixerControlInfo
    {
        public MixerControlInfo(string controlName, MixerControlChannelInfo[] channels)
        {
            ControlName = controlName; Channels = channels;
        }
        public string ControlName { get; }
        public MixerControlChannelInfo[] Channels { get; }
    }
}
