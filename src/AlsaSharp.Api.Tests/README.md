# AlsaSharp.Api.Tests

Comprehensive test suite for the AlsaSharp.Api project using NUnit and NSubstitute.

## Overview

This test project provides integration tests for all API endpoints and verifies dependency injection configuration using `WebApplicationFactory<Program>` and NSubstitute for mocking.

## Test Files

### Core Tests

- **HealthCheckEndpointTests.cs** - Tests for the `/health` endpoint
  - Verifies 200 OK status code
  - Validates "Healthy" status response

- **ServiceResolutionTests.cs** - Tests for dependency injection configuration
  - Verifies all operation services can be resolved
  - Individual tests for each operation service (Loopback, SNR, TestTone, AudioLevels, CopyOnly, AudioCards)

### Endpoint Tests

#### Comprehensive Implementation

- **LoopbackTestEndpointTests.cs** - Tests for `/api/loopback-test` endpoint
  - Valid request returns 200 OK
  - Valid request returns correct response structure
  - Failed test returns 400 Bad Request
  - Service exception returns 500 Internal Server Error
  - Service exception returns problem details
  - Request parameters are passed correctly to operation
  - Cancellation is handled properly
  - Default values are used for optional parameters

#### Basic Implementation (with TODO comments for expansion)

- **SNRTestEndpointTests.cs** - Tests for `/api/snr-test` endpoint
- **TestToneEndpointTests.cs** - Tests for `/api/test-tone` endpoint
- **AudioLevelsEndpointTests.cs** - Tests for `/api/audio-levels` endpoint
- **CopyOnlyEndpointTests.cs** - Tests for `/api/copy-only` endpoint
- **AudioCardsEndpointTests.cs** - Tests for `/api/audio-cards` endpoint

## Test Pattern

All endpoint tests follow the same integration testing pattern:

```csharp
[TestFixture]
public class {Endpoint}Tests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private I{Operation} _mockOperation = null!;

    [SetUp]
    public void Setup()
    {
        _mockOperation = Substitute.For<I{Operation}>();
        
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove real service and add mock
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(I{Operation}));
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

    // Tests...
}
```

## Key Technologies

- **NUnit 4.2.2** - Testing framework
- **NSubstitute 5.3.0** - Mocking framework
- **Microsoft.AspNetCore.Mvc.Testing 9.0.0** - Integration testing support

## Running Tests

```bash
cd src/AlsaSharp.Api.Tests
dotnet test
```

## Test Coverage

Currently, the test suite includes:
- ✅ 2 tests for health check endpoint
- ✅ 7 tests for service resolution
- ✅ 9 comprehensive tests for loopback endpoint
- ✅ 1 test each for other endpoints (5 endpoints)

**Total: 23 tests, all passing**

## Future Enhancements

The following endpoint tests have TODO comments for additional test coverage:
- SNRTestEndpoint
- TestToneEndpoint
- AudioLevelsEndpoint
- CopyOnlyEndpoint
- AudioCardsEndpoint

Each should include tests for:
- Valid requests
- Invalid requests (400 Bad Request)
- Service exceptions (500 Internal Server Error)
- Response serialization
- Request parameter validation

## Notes

- All tests use mocked operation services to avoid dependencies on actual ALSA hardware
- The `Program` class was made accessible for testing by adding `public partial class Program { }`
- Tests verify HTTP status codes, response serialization, and proper error handling
