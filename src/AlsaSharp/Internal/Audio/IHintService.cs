namespace AlsaSharp.Internal.Audio
{
    /// <summary>
    /// Service interface exposing ALSA hints and card information.
    /// </summary>
    public interface IHintService
    {
        /// <summary>All discovered hints.</summary>
        List<Hint> Hints { get; }
        /// <summary>All discovered card infos.</summary>
        List<CardInfo> CardInfos { get; }
        /// <summary>Return canonical DTOs that mirror the shape produced by alsactl parsing.</summary>
        List<CtlCardDto> GetAlsactlCards();
        /// <summary>Return canonical hint DTOs for comparison.</summary>
        List<HintDto> GetCanonicalHints();
    }
}