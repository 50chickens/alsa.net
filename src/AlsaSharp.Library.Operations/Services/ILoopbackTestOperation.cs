using AlsaSharp.Library.Operations.Models;

namespace AlsaSharp.Library.Operations.Services;

public interface ILoopbackTestOperation
{
    Task<LoopbackTestResponse> ExecuteAsync(LoopbackTestRequest request, CancellationToken cancellationToken = default);
}
