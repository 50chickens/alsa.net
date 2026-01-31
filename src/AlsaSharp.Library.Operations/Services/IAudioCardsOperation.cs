using AlsaSharp.Library.Operations.Models;

namespace AlsaSharp.Library.Operations.Services;

public interface IAudioCardsOperation
{
    Task<List<AudioCardDto>> GetAudioCardsAsync(CancellationToken cancellationToken = default);
}
