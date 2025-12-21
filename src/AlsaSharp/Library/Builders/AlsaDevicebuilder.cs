namespace AlsaSharp.Library.Builders;

/// <summary>
/// Connect your sound device configuration to a virtual interface.
/// </summary>
public static class AlsaDeviceBuilder
{
    /// <summary>
    /// Build a sound device.
    /// Use <see cref="SoundDeviceSettings"/> for parameter set up.
    /// </summary>
    /// <param name="settings">Sound device configuration to use.</param>
    /// <returns>Sound device ready to use.</returns>
    public static ISoundDevice Build(SoundDeviceSettings settings)
        => new UnixSoundDevice(settings);
}
