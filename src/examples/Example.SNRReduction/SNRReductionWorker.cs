using System.Text.Json;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using AlsaSharp.Library.Logging;
using Example.SNRReduction.Models;
using Example.SNRReduction.Services;
using Example.SNRReduction.Interfaces;
using Example.SNRReduction.Audio;
using AlsaSharp;
using Microsoft.Extensions.Options;

namespace Example.SNRReduction;

/// <summary>
/// Hosted worker that runs the baseline measurement and prints results to the console.
/// </summary>
public class SNRReductionWorker : BackgroundService
{
    private readonly ILog<SNRReductionWorker> _log;
    
    private readonly AudioLevelMeterRecorderServiceOptions _recorderOptions;
    private readonly SNRReductionServiceOptions _snrReductionServiceOptions;
    private readonly IControlSweepService _sweepService;
    private readonly IEnumerable<ISoundDevice> _soundDevices;
    private readonly IServiceProvider _services;
    private readonly ISNRMeasurementService _alsabatService;
    private readonly ISNRWorkerHelper _helper;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly string _timestamp;
    
    private TimeSpan _measureDuration;
    private int _measurementCount;

    public SNRReductionWorker(ILog<SNRReductionWorker> log,
        IOptions<AudioLevelMeterRecorderServiceOptions> recorderOptions,
        IOptions<SNRReductionServiceOptions> options,
        IHostApplicationLifetime lifetime,
        IControlSweepService sweepService,
        IEnumerable<ISoundDevice> soundDevices,
        IServiceProvider services,
        ISNRMeasurementService alsabatService,
        ISNRWorkerHelper helper)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _recorderOptions = recorderOptions?.Value ?? new AudioLevelMeterRecorderServiceOptions(3, 1, "Baseline recording");
        _snrReductionServiceOptions = options?.Value ?? new SNRReductionServiceOptions();
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        _sweepService = sweepService ?? throw new ArgumentNullException(nameof(sweepService));
        _soundDevices = soundDevices ?? throw new ArgumentNullException(nameof(soundDevices));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _alsabatService = alsabatService ?? throw new ArgumentNullException(nameof(alsabatService));
        _helper = helper ?? throw new ArgumentNullException(nameof(helper));
        _timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.Trace("SNRReductionWorker starting baseline measurement...");

        // Force a single baseline measurement of 15 seconds for quicker diagnostics
        _measureDuration = TimeSpan.FromSeconds(15);
        _measurementCount = 1;

        // Resolve measurement folder from options and ensure it exists
        var measurementFolder = _snrReductionServiceOptions?.MeasurementFolder ?? "~/.SNRReduction";
        measurementFolder = measurementFolder.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        Directory.CreateDirectory(measurementFolder);

        // Apply ALSA state file per-device if configured (use library restore, not alsactl)
        var applyAlsaState = _snrReductionServiceOptions!.ApplyAlsaStateFile;
        if (!string.IsNullOrWhiteSpace(applyAlsaState))
        {
            ApplyAlsaState();
        }

        // Perform loopback tone tests via LoopbackTester service when enabled
        if (_snrReductionServiceOptions.MeasureSNR)
        {
            await RunLoopbackToneTestsAsync(stoppingToken);
        }

        _log.Trace($"Starting measurement: duration={_measureDuration.TotalSeconds}s count={_measurementCount} for {_soundDevices.Count()} devices");

        // Run continuous Alsabat-style SNR measurements via ISNRMonitorService when enabled
        if (_snrReductionServiceOptions.MeasureSNR)
        {
            await MeasureSNROnAllDevices(measurementFolder, stoppingToken);
        }

        foreach (var device in _soundDevices)
        {
            if (device == null) continue;
            var settings = device.Settings;
            if (settings == null) continue;

            // Prefer baseline file path created by the builder; fallback to worker naming if absent
            var jsonPath = settings.BaselineFilePath;
            if (string.IsNullOrWhiteSpace(jsonPath))
            {
                var cardId = settings.CardId ?? "unknown";
                var cardName = settings.CardName ?? settings.RecordingDeviceName ?? "unknown";
                var fileBase = _helper.SanitizeFileName(cardName ?? cardId ?? "unknown");
                jsonPath = Path.Combine(measurementFolder, $"baseline_{_timestamp}_{fileBase}.json");
            }

            _log.Trace("Running baseline measurement (appending to header file)");

            try
            {
                MeasureAudioLevelsAndSaveMeasurements(device, jsonPath);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to run baseline");
            }
        }
        
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

    private void MeasureAudioLevelsAndSaveMeasurements(ISoundDevice device, string jsonPath)
    {
        if (_snrReductionServiceOptions.MeasureAudioLevels)
        {
            // Create per-device meter and recorder instances only when requested
            var meterLog = _services.GetRequiredService<ILog<AudioInterfaceLevelMeter>>();
            var recorderLog = _services.GetRequiredService<ILog<AudioLevelMeterRecorderService>>();
            var meter = new AudioInterfaceLevelMeter(device, meterLog);
            var recorder = new AudioLevelMeterRecorderService(recorderLog, meter, _recorderOptions);

            var results = recorder.GetAudioMeterLevelReadings(_measureDuration, _measurementCount, jsonPath);
            _log.Info($"Baseline completed and written to {jsonPath}");
        }
        else
        {
            _log.Info("MeasureAudioLevels is false; skipping audio level baseline recording.");
        }
    }

    private async Task MeasureSNROnAllDevices(string measurementFolder, CancellationToken stoppingToken)
    {
        try
        {
            var monitor = _services.GetService<ISNRMonitorService>();
            if (monitor != null)
            {
                foreach (var device in _soundDevices)
                {
                    try
                    {
                        if (device == null) continue;
                        var settings = device.Settings;
                        if (settings == null) continue;
                        _log.Info($"Starting continuous SNR monitoring on device {settings.RecordingDeviceName} (15 samples, 1s each)");
                        int samples = 15;
                        await monitor.RunContinuousMonitoringAsync(device, _measureDuration, samples, measurementFolder, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"Continuous SNR monitoring failed for device {device?.Settings?.RecordingDeviceName}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _log.Trace($"Continuous SNR monitoring failed: {ex.Message}");
        }
    }

    private async Task RunLoopbackToneTestsAsync(CancellationToken stoppingToken)
    {
        var tester = _services.GetService<ILoopbackTester>();
        if (tester != null)
        {
            foreach (var device in _soundDevices)
            {
                try
                {
                    if (device == null) continue;
                    var settings = device.Settings;
                    if (settings == null) continue;
                    _log.Info($"Running loopback tone test on card {settings.CardId ?? settings.CardName ?? "unknown"}");
                    await tester.RunLoopbackTestAsync(device, stoppingToken);
                }
                catch (Exception ex)
                {
                    _log.Warn($"Loopback test failed for device {device?.Settings?.CardId}: {ex.Message}");
                }
            }
        }
    }

    private void ApplyAlsaState()
    {
        _log.Info("Applying ALSA state file before measurement as configured for all discovered cards.");

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string folderPath = _snrReductionServiceOptions.DefaultAudioStateFolderName?.Replace("~", home) ?? home;

        // If the configured value looks like a full path (rooted or contains a separator), use it
        // otherwise combine with the default folder.
        string requested = _snrReductionServiceOptions.ApplyAlsaStateFile;
        string stateFilePath;
        try
        {
            if (Path.IsPathRooted(requested) || requested.Contains(Path.DirectorySeparatorChar) || requested.Contains('/'))
            {
                stateFilePath = requested.Replace("~", home);
            }
            else
            {
                stateFilePath = Path.Combine(folderPath, requested);
            }

            foreach (var dev in _soundDevices)
            {
                if (dev == null) continue;
                try
                {
                    var dsettings = dev.Settings;
                    _log.Info($"Applying ALSA state for device {dsettings?.RecordingDeviceName} from: {stateFilePath}");
                    dev.RestoreStateFromAlsaStateFile(stateFilePath);
                }
                catch (Exception ex)
                {
                    _log.Warn($"Failed to apply ALSA state for device {dev?.Settings?.RecordingDeviceName}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _log.Warn($"Failed to resolve/apply ALSA state file '{_snrReductionServiceOptions.ApplyAlsaStateFile}': {ex.Message}");
        }
    }



}
