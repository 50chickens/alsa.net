using System.Text.Json;
using AlsaSharp.Library.Logging;
using Example.SNRReduction.Models;
using Example.SNRReduction.Services;
using Microsoft.Extensions.Options;

namespace Example.SNRReduction;

/// <summary>
/// Hosted worker that runs the baseline measurement and prints results to the console.
/// </summary>
public class SNRReductionWorker : BackgroundService
{
    private readonly ILog<SNRReductionWorker> _log;
    private readonly IAudioLevelMeterRecorderService _recorder;
    private readonly AudioLevelMeterRecorderServiceOptions _recorderOptions;
    private readonly SNRReductionServiceOptions _options;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly string _timestamp;
    private string fileNameToStoreMeasurements = "";

    
    public SNRReductionWorker(ILog<SNRReductionWorker> log,
        IAudioLevelMeterRecorderService recorder,
        IOptions<AudioLevelMeterRecorderServiceOptions> recorderOptions,
        IOptions<SNRReductionServiceOptions> options,
        IHostApplicationLifetime lifetime)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        _recorderOptions = recorderOptions?.Value ?? new AudioLevelMeterRecorderServiceOptions(3, 1, "Baseline recording");
        _options = options?.Value ?? new SNRReductionServiceOptions();
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        _timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.Info("SNRReductionWorker starting baseline measurement...");

        var measureDuration = TimeSpan.FromSeconds(_recorderOptions.MeasurementDuration);
        var measurementCount = _recorderOptions.MeasurementCount;

        // Run the measurement on a background task and enforce a reasonable timeout so the host doesn't hang.
        TimeSpan overallTimeout = TimeSpan.FromSeconds(Math.Max(10, measurementCount * measureDuration.TotalSeconds + 5));

        _log.Info($"Starting measurement: duration={measureDuration.TotalSeconds}s count={measurementCount} timeout={overallTimeout.TotalSeconds}s");

        // Run the recorder (which returns a List<AudioMeterLevelReading>) on a background task.
        var measurementTask = Task.Run(() => _recorder.GetAudioMeterLevelReadings(measureDuration, measurementCount, _recorderOptions.Description), stoppingToken);

        var completed = await Task.WhenAny(measurementTask, Task.Delay(overallTimeout, stoppingToken));

        List<AudioMeterLevelReading> readings;
        if (completed == measurementTask && measurementTask.Status == TaskStatus.RanToCompletion)
        {
            readings = measurementTask.Result;
            _log.Info("Baseline measurement complete.");
        }
        else if (measurementTask.IsFaulted)
        {
            _log.Error(measurementTask.Exception, "Baseline measurement failed with exception");
            readings = new List<AudioMeterLevelReading>();
        }
        else
        {
            _log.Warn("Baseline measurement timed out or was cancelled.");
            readings = new List<AudioMeterLevelReading>();
        }

        var optionsJson = new JsonSerializerOptions { WriteIndented = true };
        Console.WriteLine(JsonSerializer.Serialize(new { Baseline = readings }, optionsJson));

        // Signal the host to stop.
        _lifetime.StopApplication();

        await Task.CompletedTask;
    }
}
