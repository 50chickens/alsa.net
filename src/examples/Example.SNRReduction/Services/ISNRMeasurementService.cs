using AlsaSharp;

namespace Example.SNRReduction.Services;

public interface ISNRMeasurementService
{
    void MeasureSNR(ISoundDevice device, int targetFrequencyHz, CancellationToken stoppingToken);
    
    /// <summary>
    /// Analyze a single-channel float PCM buffer for SNR against a target sine frequency.
    /// </summary>
    /// <param name="samples">Mono float samples (signed, approximately -1..1)</param>
    /// <param name="sampleRate">Sample rate in Hz</param>
    /// <param name="targetFreq">Target sine frequency in Hz</param>
    /// <returns>Aggregated SNR analysis result.</returns>
    SNRAnalysisResult MeasureSNRforAudioDevice(float[] samples, ISoundDevice device, double targetFrequencyHz, int originalFrames = 0);
}
