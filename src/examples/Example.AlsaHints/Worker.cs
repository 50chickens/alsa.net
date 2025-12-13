using System.Text.Json;
using AlsaSharp.Library.Services;

namespace Example.AlsaHints;

/// <summary>
/// Worker service for ALSA hints.
/// </summary>
public class AlsaHintWorker(ILogger<AlsaHintWorker> log, IHintService alsaHintService, IHostApplicationLifetime lifetime) : BackgroundService
{
    private readonly ILogger<AlsaHintWorker> _log = log;
    private readonly IHintService _alsaHintService = alsaHintService;
    private readonly IHostApplicationLifetime _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            _log.LogInformation("Getting ALSA cards...");
            var cards = _alsaHintService.GetAlsactlCards();
            _log.LogInformation("Getting ALSA hints...");
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
