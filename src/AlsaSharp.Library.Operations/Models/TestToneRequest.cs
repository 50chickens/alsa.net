namespace AlsaSharp.Library.Operations.Models;

public record TestToneRequest(
    CardSelector CardSelector,
    int FrequencyHz = 1000,
    double AmplitudeDbfs = -12.0,
    int DurationMs = 1000
);
