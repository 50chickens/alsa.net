namespace AlsaSharp.Library.Operations.Models;

public record LoopbackTestRequest(
    CardSelector CardSelector,
    int TestDurationMs = 3000,
    double TestLevelDbfs = -12.0
);
