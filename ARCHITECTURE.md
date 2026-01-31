# AlsaSharp Architecture

This document describes the new architecture of the AlsaSharp project after the refactoring.

## Overview

The AlsaSharp project has been refactored into a modular architecture with three main components:

1. **AlsaSharp.Library.Operations** - Business logic and operations
2. **AlsaSharp.Api** - REST API server
3. **AlsaSharp.Console.Consolonia** - Terminal UI client

## Project Structure

```
src/
├── AlsaSharp.Library.Operations/      # Business logic layer
│   ├── Models/                         # DTOs and request/response models
│   └── Services/                       # Operation services
│
├── AlsaSharp.Api/                      # REST API server
│   ├── Extensions/                     # DI configuration
│   ├── Program.cs                      # Minimal API setup
│   └── appsettings.json               # Configuration
│
├── AlsaSharp.Console.Consolonia/       # Terminal UI client
│   ├── Services/                       # API client and navigation
│   ├── ViewModels/                     # MVVM ViewModels
│   └── Views/                          # AXAML views
│
├── AlsaSharp.Api.Tests/                # API tests
└── AlsaSharp.Library.Operations.Tests/ # Operations tests
```

## Components

### AlsaSharp.Library.Operations

**Purpose**: Contains all business logic and audio processing operations.

**Key Features**:
- Async operation interfaces (ILoopbackTestOperation, ISNRTestOperation, etc.)
- DTO models for request/response (record types)
- Audio processing services (loopback test, SNR measurement, test tone, etc.)
- No ALSA-specific types in public APIs

**Dependencies**:
- AlsaSharp (core ALSA wrapper)
- AlsaSharp.Core (abstractions)
- Microsoft.Extensions.DependencyInjection.Abstractions
- Microsoft.Extensions.Options

### AlsaSharp.Api

**Purpose**: REST API server that exposes audio operations as HTTP endpoints.

**Key Features**:
- Minimal API (ASP.NET Core)
- HTTP-only binding to localhost:5000
- Swagger/OpenAPI documentation
- CORS support for localhost development
- Health check endpoint
- Global exception handling with ProblemDetails
- Structured logging with ILog<T>

**Endpoints**:
- `POST /api/loopback-test` - Run loopback latency test
- `POST /api/snr-test` - Measure signal-to-noise ratio
- `POST /api/test-tone` - Generate test tone playback
- `POST /api/audio-levels` - Record and measure audio levels
- `POST /api/copy-only` - Copy audio input to output
- `GET /api/audio-cards` - List available audio cards
- `GET /health` - Health check

**Configuration**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "Cors": {
    "Origins": ["http://localhost:3000", "http://localhost:5173"]
  },
  "Swagger": {
    "Enabled": true
  }
}
```

**Environment Variables**:
- `ALSASHARP_ApiBaseUrl` - Override API base URL
- `ALSASHARP_Logging__LogLevel__Default` - Set log level

### AlsaSharp.Console.Consolonia

**Purpose**: Terminal UI client for interacting with the API.

**Key Features**:
- Avalonia UI framework with Consolonia theme
- MVVM pattern with ReactiveUI
- HTTP client for API communication
- Navigation service for view switching
- Main menu with keyboard navigation
- Test execution views

**Views**:
- MainView - Main menu
- LoopbackTestView - Loopback test configuration and results

**Configuration**:
```json
{
  "ApiBaseUrl": "http://localhost:5000"
}
```

## Running the Application

### Prerequisites

- .NET 9.0 SDK
- Linux (for ALSA support)

### Start the API Server

```bash
cd src/AlsaSharp.Api
dotnet build
dotnet run
```

Or use the launch script:
```bash
./start-api.sh
```

The API will be available at http://localhost:5000

### Start the Console Application

```bash
cd src/AlsaSharp.Console.Consolonia
dotnet build
dotnet run
```

Or use the launch script:
```bash
./start-console.sh
```

## Development

### Adding a New Operation

1. Create request/response models in `AlsaSharp.Library.Operations/Models/`
2. Create operation interface in `AlsaSharp.Library.Operations/Services/`
3. Implement operation service
4. Register service in `AlsaSharp.Api/Extensions/ServiceCollectionExtensions.cs`
5. Add endpoint in `AlsaSharp.Api/Program.cs`
6. Create tests in `AlsaSharp.Api.Tests/`

### Running Tests

```bash
# Run all tests
dotnet test

# Run API tests only
cd src/AlsaSharp.Api.Tests
dotnet test

# Run Operations tests only
cd src/AlsaSharp.Library.Operations.Tests
dotnet test
```

## Deployment

### API as Self-Contained Executable

```bash
cd src/AlsaSharp.Api
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
```

### Console as Framework-Dependent

```bash
cd src/AlsaSharp.Console.Consolonia
dotnet publish -c Release -r linux-x64
```

### Systemd Service (API)

Create `/etc/systemd/system/alsasharp-api.service`:

```ini
[Unit]
Description=AlsaSharp API Server
After=network.target

[Service]
Type=simple
User=alsasharp
WorkingDirectory=/opt/alsasharp/api
ExecStart=/opt/alsasharp/api/AlsaSharp.Api
Restart=on-failure
Environment="ALSASHARP_Logging__LogLevel__Default=Information"

[Install]
WantedBy=multi-user.target
```

Enable and start:
```bash
sudo systemctl enable alsasharp-api
sudo systemctl start alsasharp-api
```

## Testing

### Unit Tests

- **AlsaSharp.Api.Tests**: Tests for API endpoints, DI resolution, and integration
- **AlsaSharp.Library.Operations.Tests**: Tests for operation services (TODO)

### Integration Testing Pattern

```csharp
[TestFixture]
public class MyEndpointTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Mock dependencies
                    var mockOperation = Substitute.For<IMyOperation>();
                    mockOperation.ExecuteAsync(Arg.Any<MyRequest>(), Arg.Any<CancellationToken>())
                        .Returns(new MyResponse(true, "Success", null));
                    services.AddScoped(_ => mockOperation);
                });
            });
        _client = _factory.CreateClient();
    }

    [Test]
    public async Task MyEndpoint_ValidRequest_Returns200OK()
    {
        var request = new MyRequest(...);
        var response = await _client.PostAsJsonAsync("/api/my-endpoint", request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
```

## Code Style

- Nullable reference types enabled
- File-scoped namespaces
- Primary constructors for dependency injection
- Async methods end with `Async` suffix
- Record types for immutable DTOs
- Expression-bodied members for simple methods
- ILog<T> for logging (not ILogger<T>)

## TODO / Future Improvements

1. **Device Mapping**: Implement CardSelector to ISoundDevice mapping in operations
2. **Additional Views**: Implement SNRTestView, TestToneView, AudioLevelsView
3. **Validation**: Add FluentValidation for request validation
4. **Operations Tests**: Add comprehensive tests for Goertzel, THD, audio processing
5. **Cancellation**: Full cancellation support for long-running operations
6. **Configuration**: Full IOptions pattern with validation
7. **Authentication**: Add API authentication/authorization
8. **Metrics**: Add metrics/telemetry support

## Troubleshooting

### API won't start
- Check port 5000 is not in use: `netstat -tuln | grep 5000`
- Check logs in console output
- Verify .NET 9.0 SDK is installed: `dotnet --version`

### Console can't connect to API
- Verify API is running: `curl http://localhost:5000/health`
- Check `appsettings.json` has correct API base URL
- Check firewall settings

### Tests failing
- Run `dotnet build` first
- Check for missing dependencies
- Verify test project references are correct
