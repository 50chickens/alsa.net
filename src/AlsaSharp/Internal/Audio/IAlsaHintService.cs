/// <summary>
/// Service interface exposing ALSA hints and card information.
/// </summary>
public interface IAlsaHintService
{
    /// <summary>All discovered hints.</summary>
    List<AlsaHint> Hints { get; }
    /// <summary>All discovered card infos.</summary>
    List<AlsaCardInfo> CardInfos { get; }
    /// <summary>Return canonical DTOs that mirror the shape produced by alsactl parsing.</summary>
    List<AlsactlCardDto> GetAlsactlCards();
    /// <summary>Return canonical hint DTOs for comparison.</summary>
    List<AlsaHintDto> GetCanonicalHints();
}
