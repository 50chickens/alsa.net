using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Example.AlsaHints;

public class AlsaHintWorker : BackgroundService
{
    private readonly ILogger<AlsaHintWorker> _log;
    private readonly IAlsaHintService _alsaHintService;
    private readonly IHostApplicationLifetime _lifetime;
    
    public AlsaHintWorker(ILogger<AlsaHintWorker> log, IAlsaHintService alsaHintService, IHostApplicationLifetime lifetime)
    {
        _log = log;
        _alsaHintService = alsaHintService;
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Prefer Console.WriteLine for terse output rather than the default log prefix
        try
        {
            _log.LogInformation(JsonConvert.SerializeObject(_alsaHintService.Hints, Formatting.Indented));
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }
}
