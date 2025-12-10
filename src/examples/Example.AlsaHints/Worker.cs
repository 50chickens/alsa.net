using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

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
        try
        {
            // Produce canonical JSON to stdout (no --outdir option)
            var options = new JsonSerializerOptions { WriteIndented = true };
            var cards = _alsaHintService.GetAlsactlCards();
            var hints = _alsaHintService.GetCanonicalHints();
            var combined = new { Alsactl = cards, Hints = hints };
            Console.WriteLine(JsonSerializer.Serialize(combined, options));
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }
}
