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
        IServiceProvider services)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _recorderOptions = recorderOptions?.Value ?? new AudioLevelMeterRecorderServiceOptions(3, 1, "Baseline recording");
        _snrReductionServiceOptions = options?.Value ?? new SNRReductionServiceOptions();
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        _sweepService = sweepService ?? throw new ArgumentNullException(nameof(sweepService));
        _soundDevices = soundDevices ?? throw new ArgumentNullException(nameof(soundDevices));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.Info("SNRReductionWorker starting baseline measurement...");

        _measureDuration = TimeSpan.FromSeconds(_recorderOptions.MeasurementDuration);
        _measurementCount = _recorderOptions.MeasurementCount;

        // Resolve measurement folder from options and ensure it exists
        var measurementFolder = _snrReductionServiceOptions?.MeasurementFolder ?? "~/.SNRReduction";
        measurementFolder = measurementFolder.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        Directory.CreateDirectory(measurementFolder);

        // Apply ALSA state file per-device if configured (use library restore, not alsactl)
        if (!string.IsNullOrWhiteSpace(_snrReductionServiceOptions.ApplyAlsaStateFile))
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
                    try
                    {
                        _log.Info($"Applying ALSA state for device {dev?.Settings?.RecordingDeviceName} from: {stateFilePath}");
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
        
        // Try to configure audio cards to a sane starting state (example: DAI Left Source MUX)
        if (_snrReductionServiceOptions.AutoConfigureDaiMux)
        {
            try
            {
                var cfg = _services.GetService<Example.SNRReduction.Services.AudioCardConfigService>();
                if (cfg != null)
                {
                    foreach (var dev in _soundDevices)
                    {
                        try
                        {
                            var cardIndex = dev?.Settings?.CardIndex ?? 0;
                            var chosen = cfg.TryPreferredDaiMux(cardIndex);
                            _log.Info($"DAI Left Source MUX for card {cardIndex} set to: {chosen ?? "(none)"}");
                        }
                        catch (Exception ex)
                        {
                            _log.Warn($"Failed to configure DAI MUX for device {dev?.Settings?.RecordingDeviceName}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"AudioCardConfigService error: {ex.Message}");
            }
        }

        // Check DAI MUX routing and warn if left/right selections differ (may cause imbalance)
        try
        {
            var left = AlsaSharp.MixerManager.GetElementValue( (int?)null ?? 0, "DAI Left Source MUX");
            var right = AlsaSharp.MixerManager.GetElementValue( (int?)null ?? 0, "DAI Right Source MUX");
            if (left.Success && right.Success && left.Type == "enum" && right.Type == "enum")
            {
                if (!string.Equals(left.Value, right.Value, StringComparison.OrdinalIgnoreCase))
                {
                    _log.Warn($"Detected DAI mux mismatch: Left='{left.Value}' Right='{right.Value}'. This may cause L/R imbalance.");
                }
            }
        }
        catch (Exception ex)
        {
            _log.Trace($"DAI MUX check failed: {ex.Message}");
        }

        _log.Info($"Starting measurement: duration={_measureDuration.TotalSeconds}s count={_measurementCount} for {_soundDevices.Count()} devices");

        // Baseline summary and per-device headers are created by the UnixSoundDeviceBuilder at registration time

        foreach (var device in _soundDevices)
        {
            var settings = device?.Settings;

            // Prefer baseline file path created by the builder; fallback to worker naming if absent
            var jsonPath = settings?.BaselineFilePath;
            if (string.IsNullOrWhiteSpace(jsonPath))
            {
                var cardId = settings?.CardId ?? "unknown";
                var cardName = settings?.CardName ?? settings?.RecordingDeviceName ?? "unknown";
                var fileBase = SanitizeFileName(cardName ?? cardId ?? "unknown");
                jsonPath = Path.Combine(measurementFolder, $"baseline_{_timestamp}_{fileBase}.json");
            }

            _log.Trace("Running baseline measurement (appending to header file)");

            // Create per-device meter and recorder instances
            var meterLog = _services.GetRequiredService<ILog<AudioInterfaceLevelMeter>>();
            var recorderLog = _services.GetRequiredService<ILog<AudioLevelMeterRecorderService>>();
            var meter = new AudioInterfaceLevelMeter(device, meterLog);
            var recorder = new AudioLevelMeterRecorderService(recorderLog, meter, _recorderOptions);

            try
            {
                var results = recorder.GetAudioMeterLevelReadings(_measureDuration, _measurementCount, jsonPath);
                _log.Info($"Baseline completed and written to {jsonPath}");
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

    private static string SanitizeFileName(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "unknown";
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Replace(' ', '_');
    }
}
