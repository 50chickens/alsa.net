namespace AlsaSharp.Library.Operations.Models;

public record SNRTestResponse(
    bool Success,
    string Message,
    SNRTestResultDto? Result
);
