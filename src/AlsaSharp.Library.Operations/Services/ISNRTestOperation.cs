using AlsaSharp.Library.Operations.Models;

namespace AlsaSharp.Library.Operations.Services;

public interface ISNRTestOperation
{
    Task<SNRTestResponse> ExecuteAsync(SNRTestRequest request, CancellationToken cancellationToken = default);
}
