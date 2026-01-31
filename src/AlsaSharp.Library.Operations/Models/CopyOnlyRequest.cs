namespace AlsaSharp.Library.Operations.Models;

public record CopyOnlyRequest(
    CardSelector CardSelector,
    int DurationMs = 5000
);
