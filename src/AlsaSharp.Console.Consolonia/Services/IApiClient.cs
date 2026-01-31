using AlsaSharp.Library.Operations.Models;

namespace AlsaSharp.Console.Consolonia.Services;

public interface IApiClient
{
    Task<LoopbackTestResponse> ExecuteLoopbackTestAsync(LoopbackTestRequest request, CancellationToken ct = default);
    Task<SNRTestResponse> ExecuteSNRTestAsync(SNRTestRequest request, CancellationToken ct = default);
    Task<TestToneResponse> ExecuteTestToneAsync(TestToneRequest request, CancellationToken ct = default);
    Task<AudioLevelsResponse> ExecuteAudioLevelsAsync(AudioLevelsRequest request, CancellationToken ct = default);
    Task<CopyOnlyResponse> ExecuteCopyOnlyAsync(CopyOnlyRequest request, CancellationToken ct = default);
    Task<List<AudioCardDto>> GetAudioCardsAsync(CancellationToken ct = default);
}
