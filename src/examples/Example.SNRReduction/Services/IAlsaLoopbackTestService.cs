#nullable enable

using AlsaSharp;
using Example.SNRReduction.Models;

namespace Example.SNRReduction.Services;

public interface IAlsaLoopbackTestService
{
    /// <summary>
    /// Tests ALSA loopback by simultaneously playing a test tone and recording it.
    /// Returns tuple of (PlayedSignalStats, RecordedSignalStats, IsLoopbackWorking, FullResult)
    /// </summary>
    (AlsaLoopbackTestResult PlayedSignal, AlsaLoopbackTestResult RecordedSignal, bool IsLoopbackWorking, LoopbackTestResult FullResult) TestLoopback(
        ISoundDevice playbackDevice, 
        ISoundDevice recordingDevice, 
        int testDurationMs = 3000,
        double testLevelDbfs = -12.0);
}

public class AlsaLoopbackTestResult
{
    public int Samples { get; set; }
    public List<double> ChannelDbfs { get; set; } = new();
    public List<double> ChannelRms { get; set; } = new();
    public List<double> PeakAmplitude { get; set; } = new();
    public double SignalToNoiseRatio { get; set; } // SNR in dB
    public double TotalHarmonicDistortionDb { get; set; } // THD in dB
    public bool HasSignal { get; set; }
    
    // Latency measurement fields
    public double RoundTripLatencyMs { get; set; } = -1.0; // -1 if not measured
    public int LatencySamples { get; set; } = -1;
    public double ConfidenceScore { get; set; } = 0.0; // 0.0-1.0, correlation strength
    public bool LatencyMeasurementValid { get; set; } = false;
    
    // Optional: separate I/O latencies (if estimable)
    public double OutputLatencyMs { get; set; } = -1.0;
    public double InputLatencyMs { get; set; } = -1.0;
    
    public override string ToString()
    {
        var dbfsStr = string.Join(", ", ChannelDbfs.Select(d => $"{d:F1}dB"));
        var rmsStr = string.Join(", ", ChannelRms.Select(r => $"{r:F6}"));
        var peakStr = string.Join(", ", PeakAmplitude.Select(p => $"{p:F6}"));
        var thdStr = double.IsFinite(TotalHarmonicDistortionDb) ? $", THD: {TotalHarmonicDistortionDb:F1}dB" : "";
        var latencyStr = RoundTripLatencyMs >= 0 ? $", Latency: {RoundTripLatencyMs:F2}ms (confidence: {ConfidenceScore:F3})" : "";
        return $"Samples: {Samples}, dBFS: [{dbfsStr}], RMS: [{rmsStr}], Peak: [{peakStr}], SNR: {SignalToNoiseRatio:F1}dB{thdStr}{latencyStr}, Signal: {(HasSignal ? "Yes" : "No")}";
    }
}
