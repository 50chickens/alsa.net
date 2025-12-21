using System.Text.Json;
using AlsaSharp.Library.Logging;
using AlsaSharp.Library.Services;

namespace Example.AlsaHints;

/// <summary>
/// Worker service for ALSA hints.
/// </summary>
public class AlsaHintWorker(ILog<AlsaHintWorker> log, IHintService alsaHintService, IHostApplicationLifetime lifetime) : BackgroundService
{
    private readonly ILog<AlsaHintWorker> _log = log;
    private readonly IHintService _alsaHintService = alsaHintService;
    private readonly IHostApplicationLifetime _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            _log.Info("Getting ALSA cards...");
            var cards = _alsaHintService.GetAlsactlCards();
            _log.Info("Getting ALSA hints...");
            var hints = _alsaHintService.GetCanonicalHints();
            var combined = new { Alsactl = cards, Hints = hints };
            var logoutput = new YamlDotNet.Serialization.Serializer().Serialize(combined);
            _log.Info(logoutput);
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }
}
