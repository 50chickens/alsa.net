namespace AlsaSharp.Internal.Audio
{
    /// <summary>
    /// Represents information about a single mixer channel (volume, range and switch state).
    /// </summary>
    public class MixerSimpleElement
    {
        // Match libasound types exactly: native-sized integers for volume/range, C long for dB, int for switch.
        /// <summary>
        /// Initializes a new instance of the <see cref="MixerSimpleElement"/> class.
        /// </summary>
        /// <param name="simpleElementName">The channel name.</param>
        /// <param name="raw">The raw volume value.</param>
        /// <param name="min">The minimum volume value.</param>
        /// <param name="max">The maximum volume value.</param>
        /// <param name="db">The dB value if available; otherwise <c>null</c>.</param>
        /// <param name="sw">The switch state if available; otherwise <c>null</c>.</param>
        public MixerSimpleElement(string simpleElementName, nint raw, nint min, nint max, long? db, int? sw)
        {
            Name = simpleElementName; Raw = raw; Min = min; Max = max; Db = db; Switch = sw;
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
}
