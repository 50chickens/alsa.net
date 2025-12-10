using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using AlsaSharp.Internal.Audio;

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
