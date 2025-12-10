namespace AlsaSharp.Internal.Audio
{

    /// <summary>
    /// Enum describing the interface type parsed from ALSA hint names.
    /// Mirrors libasound's snd_ctl_elem_iface values.
    /// </summary>
    public enum InterfaceIdentificationType
    {
        /// <summary>Card interface.</summary>
        SND_CTL_ELEM_IFACE_CARD = 0,
        /// <summary>Hardware dependent interface.</summary>
        SND_CTL_ELEM_IFACE_HWDEP = 1,
        /// <summary>Mixer interface.</summary>
        SND_CTL_ELEM_IFACE_MIXER = 2,
        /// <summary>PCM interface.</summary>
        SND_CTL_ELEM_IFACE_PCM = 3,
        /// <summary>Raw MIDI interface.</summary>
        SND_CTL_ELEM_IFACE_RAWMIDI = 4,
        /// <summary>Timer interface.</summary>
        SND_CTL_ELEM_IFACE_TIMER = 5,
        /// <summary>Sequencer interface.</summary>
        SND_CTL_ELEM_IFACE_SEQUENCER = 6,
        /// <summary>Last/unknown interface.</summary>
        SND_CTL_ELEM_IFACE_LAST = 7
    }
}