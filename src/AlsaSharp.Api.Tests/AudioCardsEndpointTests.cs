using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using AlsaSharp.Library.Operations.Models;
using AlsaSharp.Library.Operations.Services;
using NSubstitute;

namespace AlsaSharp.Api.Tests;

[TestFixture]
public class AudioCardsEndpointTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private IAudioCardsOperation _mockOperation = null!;

    [SetUp]
    public void Setup()
    {
        _mockOperation = Substitute.For<IAudioCardsOperation>();
        
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(IAudioCardsOperation));
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

    // TODO: Implement comprehensive tests for AudioCards endpoint
    // - Valid request returns 200 OK
    // - Service exception returns 500 Internal Server Error
    // - Response serialization is correct
    // - Empty list is handled correctly
    // - Multiple cards are returned correctly

    [Test]
    public async Task AudioCardsEndpoint_ValidRequest_Returns200OK()
    {
        // Arrange
        var expectedCards = new List<AudioCardDto>
        {
            new AudioCardDto("default", 0, "default", 0, "Default Audio Device")
        };

        _mockOperation.GetAudioCardsAsync(Arg.Any<CancellationToken>())
            .Returns(expectedCards);

        // Act
        var response = await _client.GetAsync("/api/audio-cards");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task AudioCardsEndpoint_EmptyList_Returns200OK()
    {
        // Arrange
        _mockOperation.GetAudioCardsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AudioCardDto>());

        // Act
        var response = await _client.GetAsync("/api/audio-cards");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
