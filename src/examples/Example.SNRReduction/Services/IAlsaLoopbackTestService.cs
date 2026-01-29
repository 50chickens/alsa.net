#nullable enable

using AlsaSharp;

namespace Example.SNRReduction.Services;

public interface IAlsaLoopbackTestService
{
    /// <summary>
    /// Tests ALSA loopback by simultaneously playing a test tone and recording it.
    /// Returns tuple of (PlayedSignalStats, RecordedSignalStats, IsLoopbackWorking)
    /// </summary>
    (AlsaLoopbackTestResult PlayedSignal, AlsaLoopbackTestResult RecordedSignal, bool IsLoopbackWorking) TestLoopback(
        ISoundDevice playbackDevice, 
        ISoundDevice recordingDevice, 
        int testDurationMs = 3000);
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
    
    public override string ToString()
    {
        var dbfsStr = string.Join(", ", ChannelDbfs.Select(d => $"{d:F1}dB"));
        var rmsStr = string.Join(", ", ChannelRms.Select(r => $"{r:F6}"));
        var peakStr = string.Join(", ", PeakAmplitude.Select(p => $"{p:F6}"));
        var thdStr = double.IsFinite(TotalHarmonicDistortionDb) ? $", THD: {TotalHarmonicDistortionDb:F1}dB" : "";
        return $"Samples: {Samples}, dBFS: [{dbfsStr}], RMS: [{rmsStr}], Peak: [{peakStr}], SNR: {SignalToNoiseRatio:F1}dB{thdStr}, Signal: {(HasSignal ? "Yes" : "No")}";
    }
}
