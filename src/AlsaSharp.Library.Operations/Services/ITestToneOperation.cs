using AlsaSharp.Library.Operations.Models;

namespace AlsaSharp.Library.Operations.Services;

public interface ITestToneOperation
{
    Task<TestToneResponse> ExecuteAsync(TestToneRequest request, CancellationToken cancellationToken = default);
}
