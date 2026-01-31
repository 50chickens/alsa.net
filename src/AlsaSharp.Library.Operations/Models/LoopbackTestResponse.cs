namespace AlsaSharp.Library.Operations.Models;

public record LoopbackTestResponse(
    bool Success,
    string Message,
    LoopbackTestResultDto? Result
);
