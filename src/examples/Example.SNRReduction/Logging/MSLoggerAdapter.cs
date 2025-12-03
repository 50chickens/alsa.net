using Microsoft.Extensions.Logging;
using System;

namespace Example.SNRReduction.Logging;

public class MSLoggerAdapter<T> : ILog<T>
{
    private readonly ILogger<T> _logger;

    public MSLoggerAdapter(ILogger<T> logger)
    {
        _logger = logger;
    }

    public void Info(string message) => _logger.LogInformation(message);
    public void Debug(string message) => _logger.LogDebug(message);
    public void Warn(string message) => _logger.LogWarning(message);
    public void Error(Exception ex, string? message = null) => _logger.LogError(ex, message);
}
