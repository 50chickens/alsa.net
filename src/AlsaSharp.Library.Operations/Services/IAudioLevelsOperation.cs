using AlsaSharp.Library.Operations.Models;

namespace AlsaSharp.Library.Operations.Services;

public interface IAudioLevelsOperation
{
    Task<AudioLevelsResponse> ExecuteAsync(AudioLevelsRequest request, CancellationToken cancellationToken = default);
}
