using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using AlsaSharp.Library.Operations.Models;
using AlsaSharp.Library.Operations.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AlsaSharp.Api.Tests;

[TestFixture]
public class LoopbackTestEndpointTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private ILoopbackTestOperation _mockOperation = null!;

    [SetUp]
    public void Setup()
    {
        _mockOperation = Substitute.For<ILoopbackTestOperation>();
        
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove real service and add mock
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(ILoopbackTestOperation));
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

    [Test]
    public async Task LoopbackTestEndpoint_ValidRequest_Returns200OK()
    {
        // Arrange
        var request = new LoopbackTestRequest(
            new CardSelector("default", "default"),
            3000,
            -12.0
        );

        var expectedResult = new LoopbackTestResultDto(
            Samples: 144000,
            ChannelDbfs: new List<double> { -12.0, -12.0 },
            ChannelRms: new List<double> { 0.25, 0.25 },
            PeakAmplitude: new List<double> { 0.5, 0.5 },
            SignalToNoiseRatio: 98.5,
            TotalHarmonicDistortionDb: -60.0,
            HasSignal: true,
            RoundTripLatencyMs: 10.5,
            LatencySamples: 504,
            ConfidenceScore: 0.95,
            LatencyMeasurementValid: true
        );

        var expectedResponse = new LoopbackTestResponse(
            true,
            "Test completed successfully",
            expectedResult
        );

        _mockOperation.ExecuteAsync(Arg.Any<LoopbackTestRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var response = await _client.PostAsJsonAsync("/api/loopback-test", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task LoopbackTestEndpoint_ValidRequest_ReturnsCorrectResponse()
    {
        // Arrange
        var request = new LoopbackTestRequest(
            new CardSelector("default", "default"),
            3000,
            -12.0
        );

        var expectedResult = new LoopbackTestResultDto(
            Samples: 144000,
            ChannelDbfs: new List<double> { -12.0, -12.0 },
            ChannelRms: new List<double> { 0.25, 0.25 },
            PeakAmplitude: new List<double> { 0.5, 0.5 },
            SignalToNoiseRatio: 98.5,
            TotalHarmonicDistortionDb: -60.0,
            HasSignal: true,
            RoundTripLatencyMs: 10.5,
            LatencySamples: 504,
            ConfidenceScore: 0.95,
            LatencyMeasurementValid: true
        );

        var expectedResponse = new LoopbackTestResponse(
            true,
            "Test completed successfully",
            expectedResult
        );

        _mockOperation.ExecuteAsync(Arg.Any<LoopbackTestRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var response = await _client.PostAsJsonAsync("/api/loopback-test", request);
        var actualResponse = await response.Content.ReadFromJsonAsync<LoopbackTestResponse>();

        // Assert
        Assert.That(actualResponse, Is.Not.Null);
        Assert.That(actualResponse.Success, Is.EqualTo(expectedResponse.Success));
        Assert.That(actualResponse.Message, Is.EqualTo(expectedResponse.Message));
        Assert.That(actualResponse.Result, Is.Not.Null);
        Assert.That(actualResponse.Result.HasSignal, Is.EqualTo(expectedResult.HasSignal));
        Assert.That(actualResponse.Result.SignalToNoiseRatio, Is.EqualTo(expectedResult.SignalToNoiseRatio));
    }

    [Test]
    public async Task LoopbackTestEndpoint_FailedTest_Returns400BadRequest()
    {
        // Arrange
        var request = new LoopbackTestRequest(
            new CardSelector("default", "default"),
            3000,
            -12.0
        );

        var failedResponse = new LoopbackTestResponse(
            false,
            "Loopback test failed",
            null
        );

        _mockOperation.ExecuteAsync(Arg.Any<LoopbackTestRequest>(), Arg.Any<CancellationToken>())
            .Returns(failedResponse);

        // Act
        var response = await _client.PostAsJsonAsync("/api/loopback-test", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task LoopbackTestEndpoint_ServiceException_Returns500InternalServerError()
    {
        // Arrange
        var request = new LoopbackTestRequest(
            new CardSelector("default", "default"),
            3000,
            -12.0
        );

        _mockOperation.ExecuteAsync(Arg.Any<LoopbackTestRequest>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Service error"));

        // Act
        var response = await _client.PostAsJsonAsync("/api/loopback-test", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    [Test]
    public async Task LoopbackTestEndpoint_ServiceException_ReturnsProblemDetails()
    {
        // Arrange
        var request = new LoopbackTestRequest(
            new CardSelector("default", "default"),
            3000,
            -12.0
        );

        _mockOperation.ExecuteAsync(Arg.Any<LoopbackTestRequest>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Service error"));

        // Act
        var response = await _client.PostAsJsonAsync("/api/loopback-test", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        Assert.That(content, Does.Contain("Service error"));
    }

    [Test]
    public async Task LoopbackTestEndpoint_PassesRequestToOperation()
    {
        // Arrange
        var request = new LoopbackTestRequest(
            new CardSelector("hw:1", "hw:1,0"),
            5000,
            -6.0
        );

        var expectedResponse = new LoopbackTestResponse(
            true,
            "Test completed",
            null
        );

        _mockOperation.ExecuteAsync(Arg.Any<LoopbackTestRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        await _client.PostAsJsonAsync("/api/loopback-test", request);

        // Assert
        await _mockOperation.Received(1).ExecuteAsync(
            Arg.Is<LoopbackTestRequest>(r =>
                r.CardSelector.CardName == "hw:1" &&
                r.CardSelector.DeviceName == "hw:1,0" &&
                r.TestDurationMs == 5000 &&
                r.TestLevelDbfs == -6.0
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Test]
    public async Task LoopbackTestEndpoint_HandlesCancellation()
    {
        // Arrange
        var request = new LoopbackTestRequest(
            new CardSelector("default", "default"),
            3000,
            -12.0
        );

        _mockOperation.ExecuteAsync(Arg.Any<LoopbackTestRequest>(), Arg.Any<CancellationToken>())
            .Throws(new OperationCanceledException());

        // Act
        var response = await _client.PostAsJsonAsync("/api/loopback-test", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    [Test]
    public async Task LoopbackTestEndpoint_WithMinimalRequest_UsesDefaultValues()
    {
        // Arrange
        var request = new LoopbackTestRequest(
            new CardSelector("default", "default")
        );

        var expectedResponse = new LoopbackTestResponse(
            true,
            "Test completed",
            null
        );

        _mockOperation.ExecuteAsync(Arg.Any<LoopbackTestRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var response = await _client.PostAsJsonAsync("/api/loopback-test", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        await _mockOperation.Received(1).ExecuteAsync(
            Arg.Is<LoopbackTestRequest>(r =>
                r.TestDurationMs == 3000 &&
                r.TestLevelDbfs == -12.0
            ),
            Arg.Any<CancellationToken>()
        );
    }
}
