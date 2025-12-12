using System.Text.Json;
using AlsaSharp.Library.Logging;
using Example.SNRReduction.Models;
using Example.SNRReduction.Services;
using Example.SNRReduction.Interfaces;
using AlsaSharp;
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
    private readonly IControlSweepService _sweepService;
    private readonly ISoundDevice _soundDevice;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly string _timestamp;
    private string fileNameToStoreMeasurements = "";

    
    public SNRReductionWorker(ILog<SNRReductionWorker> log,
        IAudioLevelMeterRecorderService recorder,
        IOptions<AudioLevelMeterRecorderServiceOptions> recorderOptions,
        IOptions<SNRReductionServiceOptions> options,
        IHostApplicationLifetime lifetime,
        IControlSweepService sweepService,
        ISoundDevice soundDevice)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        _recorderOptions = recorderOptions?.Value ?? new AudioLevelMeterRecorderServiceOptions(3, 1, "Baseline recording");
        _options = options?.Value ?? new SNRReductionServiceOptions();
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        _sweepService = sweepService ?? throw new ArgumentNullException(nameof(sweepService));
        _soundDevice = soundDevice ?? throw new ArgumentNullException(nameof(soundDevice));
        _timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.Info("SNRReductionWorker starting baseline measurement...");

        var measureDuration = TimeSpan.FromSeconds(_recorderOptions.MeasurementDuration);
        var measurementCount = _recorderOptions.MeasurementCount;

        // Restore ALSA card state from predefined state file before measurement to ensure consistent starting point
        try
        {
            const string statePath = "/home/pistomp/pi-stomp/setup/audio/0-db.state";
            _log.Info($"Restoring ALSA state from: {statePath}");
            _soundDevice.RestoreStateFromAlsactlFile(statePath);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to restore ALSA state before measurement");
        }

        // Run the measurement on a background task and enforce a reasonable timeout so the host doesn't hang.
        TimeSpan overallTimeout = TimeSpan.FromSeconds(Math.Max(10, measurementCount * measureDuration.TotalSeconds + 5));

        _log.Info($"Starting measurement: duration={measureDuration.TotalSeconds}s count={measurementCount} timeout={overallTimeout.TotalSeconds}s");

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

        // If AutoSweep is enabled, run a short quick sweep to verify end-to-end behavior.
        if (_options.AutoSweep)
        {
            _log.Info("AutoSweep enabled - running a short control sweep test...");
            // quick small sweep around 0 to test that changing controls affects measured dBFS
            int controlMin = -4096;
            int controlMax = 4096;
            int controlStep = 4096;
            TimeSpan sweepMeasureDuration = TimeSpan.FromSeconds(1);
            int sweepMeasurementCount = 1;

            try
            {
                var sweepResults = _sweepService.SweepControl(_soundDevice, string.Empty, controlMin, controlMax, controlStep, sweepMeasureDuration, sweepMeasurementCount);
                Console.WriteLine(JsonSerializer.Serialize(new { Sweep = sweepResults }, optionsJson));
            }
            catch (Exception ex)
            {
                _log.Error(ex, "AutoSweep failed");
            }
        }

        // Signal the host to stop.
        _lifetime.StopApplication();

        await Task.CompletedTask;
    }
}
