using System.Text.Json;
using System.IO;
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
    private readonly SNRReductionServiceOptions _snrReductionServiceOptions;
    private readonly IControlSweepService _sweepService;
    private readonly ISoundDevice _soundDevice;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly string _timestamp;
    
    private TimeSpan _measureDuration;
    private int _measurementCount;

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
        _snrReductionServiceOptions = options?.Value ?? new SNRReductionServiceOptions();
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        _sweepService = sweepService ?? throw new ArgumentNullException(nameof(sweepService));
        _soundDevice = soundDevice ?? throw new ArgumentNullException(nameof(soundDevice));
        _timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.Info("SNRReductionWorker starting baseline measurement...");

        _measureDuration = TimeSpan.FromSeconds(_recorderOptions.MeasurementDuration);
        _measurementCount = _recorderOptions.MeasurementCount;

        // Restore ALSA card state from predefined state file before measurement to ensure consistent starting point
        if (_snrReductionServiceOptions.RestoreAlsaStateBeforeMeasurement)
        {
            _log.Info("Restoring ALSA state before measurement as configured.");
            try
            {
                string folderPath = _snrReductionServiceOptions.DefaultAudioStateFolderName.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                string defaultStateFileName = System.IO.Path.Combine(folderPath, _snrReductionServiceOptions.DefaultAudioStateFileName);
                _log.Info($"Restoring ALSA state from: {defaultStateFileName}");
                _soundDevice.RestoreStateFromAlsactlFile(defaultStateFileName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to restore ALSA state before measurement", ex);
            }
        }
        
        _log.Info($"Starting measurement: duration={_measureDuration.TotalSeconds}s count={_measurementCount}");
        _recorder.GetAudioMeterLevelReadings(_measureDuration, _measurementCount, _recorderOptions.Description);            
        
        // If AutoSweep is enabled, run a short quick sweep to verify end-to-end behavior.
        // if (_snrReductionServiceOptions.AutoSweep)
        // {
        //     _log.Info("AutoSweep enabled - running a short control sweep test...");
        //     // quick small sweep around 0 to test that changing controls affects measured dBFS
        //     int controlMin = -4096;
        //     int controlMax = 4096;
        //     int controlStep = 4096;
        //     TimeSpan sweepMeasureDuration = TimeSpan.FromSeconds(1);
        //     int sweepMeasurementCount = 1;

        //     try
        //     {
        //         var sweepResults = _sweepService.SweepControl(_soundDevice, string.Empty, controlMin, controlMax, controlStep, sweepMeasureDuration, sweepMeasurementCount);
        //         _log.Info(JsonSerializer.Serialize(new { Sweep = sweepResults }, optionsJson));
        //     }
        //     catch (Exception ex)
        //     {
        //         _log.Error(ex, "AutoSweep failed");
        //     }
        // }

        // Signal the host to stop.
        _lifetime.StopApplication();

        await Task.CompletedTask;
    }
}
