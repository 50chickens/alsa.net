using AlsaSharp;

namespace AlsaSharp.Library.Operations.Services;

public interface ISNRMeasurementService
{
    void MeasureSNR(ISoundDevice device, int targetFrequencyHz, CancellationToken stoppingToken);
    
    /// <summary>
    /// Analyze a single-channel float PCM buffer for SNR against a target sine frequency.
    /// </summary>
    /// <param name="samples">Mono float samples (signed, approximately -1..1)</param>
    /// <param name="device">Sound device with sample rate and channel information</param>
    /// <param name="targetFrequencyHz">Target sine frequency in Hz</param>
    /// <param name="originalFrames">Original frame count (0 to use samples length)</param>
    /// <returns>Aggregated SNR analysis result.</returns>
    SNRAnalysisResult MeasureSNRforAudioDevice(float[] samples, ISoundDevice device, double targetFrequencyHz, int originalFrames = 0);
}
