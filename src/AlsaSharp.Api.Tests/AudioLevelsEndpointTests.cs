using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using AlsaSharp.Library.Operations.Models;
using AlsaSharp.Library.Operations.Services;
using NSubstitute;

namespace AlsaSharp.Api.Tests;

[TestFixture]
public class AudioLevelsEndpointTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private IAudioLevelsOperation _mockOperation = null!;

    [SetUp]
    public void Setup()
    {
        _mockOperation = Substitute.For<IAudioLevelsOperation>();
        
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(IAudioLevelsOperation));
                    if (descriptor != null)
                        services.Remove(descriptor);
                    
                    services.AddScoped(_ => _mockOperation);
                });
            });
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    // TODO: Implement comprehensive tests for AudioLevels endpoint
    // - Valid request returns 200 OK
    // - Invalid request returns 400 Bad Request
    // - Service exception returns 500 Internal Server Error
    // - Response serialization is correct
    // - Request parameters are passed correctly to operation

    [Test]
    public async Task AudioLevelsEndpoint_ValidRequest_Returns200OK()
    {
        // Arrange
        var request = new AudioLevelsRequest(
            new CardSelector("default", "default"),
            3000
        );

        var expectedResponse = new AudioLevelsResponse(
            true,
            "Audio levels captured successfully",
            new List<AudioLevelReadingDto>()
        );

        _mockOperation.ExecuteAsync(Arg.Any<AudioLevelsRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var response = await _client.PostAsJsonAsync("/api/audio-levels", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
