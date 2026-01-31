using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using AlsaSharp.Library.Operations.Models;
using AlsaSharp.Library.Operations.Services;
using NSubstitute;

namespace AlsaSharp.Api.Tests;

[TestFixture]
public class TestToneEndpointTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private ITestToneOperation _mockOperation = null!;

    [SetUp]
    public void Setup()
    {
        _mockOperation = Substitute.For<ITestToneOperation>();
        
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(ITestToneOperation));
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

    // TODO: Implement comprehensive tests for TestTone endpoint
    // - Valid request returns 200 OK
    // - Invalid request returns 400 Bad Request
    // - Service exception returns 500 Internal Server Error
    // - Response serialization is correct
    // - Request parameters are passed correctly to operation

    [Test]
    public async Task TestToneEndpoint_ValidRequest_Returns200OK()
    {
        // Arrange
        var request = new TestToneRequest(
            new CardSelector("default", "default"),
            1000,
            -12.0,
            3000
        );

        var expectedResponse = new TestToneResponse(
            true,
            "Test tone played successfully"
        );

        _mockOperation.ExecuteAsync(Arg.Any<TestToneRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var response = await _client.PostAsJsonAsync("/api/test-tone", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
