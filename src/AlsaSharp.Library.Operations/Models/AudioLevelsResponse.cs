namespace AlsaSharp.Library.Operations.Models;

public record AudioLevelsResponse(
    bool Success,
    string Message,
    List<AudioLevelReadingDto>? Readings
);
