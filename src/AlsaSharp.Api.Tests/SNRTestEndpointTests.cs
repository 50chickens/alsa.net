using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using AlsaSharp.Library.Operations.Models;
using AlsaSharp.Library.Operations.Services;
using NSubstitute;

namespace AlsaSharp.Api.Tests;

[TestFixture]
public class SNRTestEndpointTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private ISNRTestOperation _mockOperation = null!;

    [SetUp]
    public void Setup()
    {
        _mockOperation = Substitute.For<ISNRTestOperation>();
        
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(ISNRTestOperation));
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

    // TODO: Implement comprehensive tests for SNR endpoint
    // - Valid request returns 200 OK
    // - Invalid request returns 400 Bad Request
    // - Service exception returns 500 Internal Server Error
    // - Response serialization is correct
    // - Request parameters are passed correctly to operation

    [Test]
    public async Task SNRTestEndpoint_ValidRequest_Returns200OK()
    {
        // Arrange
        var request = new SNRTestRequest(
            new CardSelector("default", "default"),
            1000
        );

        var expectedResponse = new SNRTestResponse(
            true,
            "Test completed",
            null
        );

        _mockOperation.ExecuteAsync(Arg.Any<SNRTestRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var response = await _client.PostAsJsonAsync("/api/snr-test", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
