namespace AlsaSharp.Library.Services;

/// <summary>
/// Interface for test tone generation and playback service.
/// </summary>
public interface ITestToneService
{
    /// <summary>
    /// Plays a test tone with the specified parameters through the given device.
    /// </summary>
    /// <param name="deviceName">The ALSA device name (e.g., "hw:CARD=sndrpihifiberry,DEV=0")</param>
    /// <param name="frequencyHz">The frequency of the tone in Hz</param>
    /// <param name="amplitudeDbfs">The amplitude in dBFS</param>
    /// <param name="leftChannelDurationMs">Duration for left channel only tone in milliseconds</param>
    /// <param name="rightChannelDurationMs">Duration for right channel only tone in milliseconds</param>
    /// <param name="bothChannelsDurationMs">Duration for both channels tone in milliseconds</param>
    void PlayTestTone(string deviceName, int frequencyHz, double amplitudeDbfs, 
        int leftChannelDurationMs, int rightChannelDurationMs, int bothChannelsDurationMs);
}
