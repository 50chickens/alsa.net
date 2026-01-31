using AlsaSharp.Library.Operations.Models;

namespace AlsaSharp.Library.Operations.Services;

public interface ICopyOnlyOperation
{
    Task<CopyOnlyResponse> ExecuteAsync(CopyOnlyRequest request, CancellationToken cancellationToken = default);
}
