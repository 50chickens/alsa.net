using System.Net.Http.Json;
using AlsaSharp.Library.Operations.Models;

namespace AlsaSharp.Console.Consolonia.Services;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LoopbackTestResponse> ExecuteLoopbackTestAsync(LoopbackTestRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/tests/loopback", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LoopbackTestResponse>(ct) 
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public async Task<SNRTestResponse> ExecuteSNRTestAsync(SNRTestRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/tests/snr", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SNRTestResponse>(ct) 
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public async Task<TestToneResponse> ExecuteTestToneAsync(TestToneRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/tests/testtone", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TestToneResponse>(ct) 
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public async Task<AudioLevelsResponse> ExecuteAudioLevelsAsync(AudioLevelsRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/tests/audiolevels", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AudioLevelsResponse>(ct) 
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public async Task<CopyOnlyResponse> ExecuteCopyOnlyAsync(CopyOnlyRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/tests/copyonly", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CopyOnlyResponse>(ct) 
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public async Task<List<AudioCardDto>> GetAudioCardsAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync("/api/audiocards", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<AudioCardDto>>(ct) 
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }
}
