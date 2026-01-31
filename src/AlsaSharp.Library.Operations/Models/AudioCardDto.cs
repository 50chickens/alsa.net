namespace AlsaSharp.Library.Operations.Models;

public record AudioCardDto(
    string CardName,
    int CardNumber,
    string DeviceName,
    int DeviceNumber,
    string Description
);
