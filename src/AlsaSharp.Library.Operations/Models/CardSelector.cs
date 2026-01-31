namespace AlsaSharp.Library.Operations.Models;

/// <summary>
/// DTO for selecting an audio card/device
/// </summary>
public record CardSelector(
    string? CardName,
    string? DeviceName,
    int? CardNumber = null,
    int? DeviceNumber = null
);
