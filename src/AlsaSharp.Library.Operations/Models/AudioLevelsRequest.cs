namespace AlsaSharp.Library.Operations.Models;

public record AudioLevelsRequest(
    CardSelector CardSelector,
    int DurationMs = 5000
);
