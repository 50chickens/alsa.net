namespace AlsaSharp.Library.Operations.Models;

public record SNRTestResultDto(
    double SignalToNoiseRatioDb,
    double TargetFrequencyHz,
    double SignalPowerDb,
    double NoisePowerDb
);
