using AlsaSharp;

namespace AlsaSharp.Library.Operations.Services;

/// <summary>
/// Service for copying audio from input channels to output channels.
/// </summary>
public interface ICopyOnlyService
{
    /// <summary>
    /// Copies audio from input channels to output channels for the specified duration.
    /// </summary>
    /// <param name="device">The audio device to use for copying.</param>
    /// <param name="durationMs">Duration of the copy operation in milliseconds.</param>
    /// <param name="stoppingToken">Cancellation token.</param>
    void CopyAudioChannels(ISoundDevice device, int durationMs = 5000, CancellationToken stoppingToken = default);
}
