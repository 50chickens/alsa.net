using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
            foreach (var hint in _alsaHintService.Hints)
            {
                Console.WriteLine(hint.Name);
                Console.WriteLine($"    {hint.CardId}, {hint.LongName}");
                Console.WriteLine($"    {hint.Description}");
                if (!string.IsNullOrWhiteSpace(hint.IOID)) Console.WriteLine($"    IOID: {hint.IOID}");
                Console.WriteLine();
            }
            // ensure the console output has time to flush
            await Task.Delay(25, stoppingToken);
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }
}
