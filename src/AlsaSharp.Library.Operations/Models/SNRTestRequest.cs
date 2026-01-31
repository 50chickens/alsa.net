namespace AlsaSharp.Library.Operations.Models;

public record SNRTestRequest(
    CardSelector CardSelector,
    int TargetFrequencyHz = 1000
);
