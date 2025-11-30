namespace Alsa.Net.Internal
{
    /// <summary>
    /// Represents information about a single mixer channel (volume, range and switch state).
    /// </summary>
    public class MixerControlChannelInfo
    {
        // Match libasound types exactly: native-sized integers for volume/range, C long for dB, int for switch.
        /// <summary>
        /// Initializes a new instance of the <see cref="MixerControlChannelInfo"/> class.
        /// </summary>
        /// <param name="name">The channel name.</param>
        /// <param name="raw">The raw volume value.</param>
        /// <param name="min">The minimum volume value.</param>
        /// <param name="max">The maximum volume value.</param>
        /// <param name="db">The dB value if available; otherwise <c>null</c>.</param>
        /// <param name="sw">The switch state if available; otherwise <c>null</c>.</param>
        public MixerControlChannelInfo(string name, nint raw, nint min, nint max, long? db, int? sw)
        {
            Name = name; Raw = raw; Min = min; Max = max; Db = db; Switch = sw;
        }

        /// <summary>
        /// Gets the name of the mixer channel.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the raw/native value for the channel volume.
        /// </summary>
        public nint Raw { get; }

        /// <summary>
        /// Gets the minimum supported volume for the channel.
        /// </summary>
        public nint Min { get; }

        /// <summary>
        /// Gets the maximum supported volume for the channel.
        /// </summary>
        public nint Max { get; }

        /// <summary>
        /// Gets the dB value for the channel if available.
        /// </summary>
        public long? Db { get; }

        /// <summary>
        /// Gets the switch state (on/off) for the channel if available.
        /// </summary>
        public int? Switch { get; }
    }

    /// <summary>
    /// Represents information about a mixer control.
    /// </summary>
    public class MixerControlInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MixerControlInfo"/> class.
        /// </summary>
        /// <param name="controlName">The name of the control.</param>
        /// <param name="channels">The channel information for the control.</param>
        public MixerControlInfo(string controlName, MixerControlChannelInfo[] channels)
        {
            ControlName = controlName; Channels = channels;
        }
        /// <summary>
        /// Gets the name of the control.
        /// </summary>
        public string ControlName { get; }
        /// <summary>
        /// Gets the channel information for the control.
        /// </summary>
        public MixerControlChannelInfo[] Channels { get; }
    }
}
