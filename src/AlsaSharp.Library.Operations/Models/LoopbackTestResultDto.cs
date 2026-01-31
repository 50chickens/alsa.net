namespace AlsaSharp.Library.Operations.Models;

public record LoopbackTestResultDto(
    int Samples,
    List<double> ChannelDbfs,
    List<double> ChannelRms,
    List<double> PeakAmplitude,
    double SignalToNoiseRatio,
    double TotalHarmonicDistortionDb,
    bool HasSignal,
    double RoundTripLatencyMs,
    int LatencySamples,
    double ConfidenceScore,
    bool LatencyMeasurementValid
);
