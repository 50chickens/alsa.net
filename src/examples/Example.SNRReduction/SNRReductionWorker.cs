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

        // Restore ALSA card state per-device if configured
        if (_snrReductionServiceOptions.RestoreAlsaStateBeforeMeasurement)
        {
            _log.Info("Restoring ALSA state before measurement as configured for all discovered cards.");
            string folderPath = _snrReductionServiceOptions.DefaultAudioStateFolderName.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            string defaultStateFileName = System.IO.Path.Combine(folderPath, _snrReductionServiceOptions.DefaultAudioStateFileName);
            foreach (var dev in _soundDevices)
            {
                try
                {
                    _log.Info($"Restoring ALSA state for device {dev?.Settings?.RecordingDeviceName} from: {defaultStateFileName}");
                    dev.RestoreStateFromAlsaStateFile(defaultStateFileName);
                }
                catch (Exception ex)
                {
                    _log.Warn($"Failed to restore ALSA state for device {dev?.Settings?.RecordingDeviceName}: {ex.Message}");
                }
            }
        }
        
        _log.Info($"Starting measurement: duration={_measureDuration.TotalSeconds}s count={_measurementCount} for {_soundDevices.Count()} devices");

        // Write a summary of discovered devices to a JSON file and log device info
        try
        {
            var summaryPath = Path.Combine(measurementFolder, $"baseline_summary_{_timestamp}.json");
            var summaryWriter = new AlsaSharp.Library.Logging.JsonWriter(summaryPath);
            var devicesList = _soundDevices.Select(d => new
            {
                CardId = d?.Settings?.CardId,
                CardName = d?.Settings?.CardName,
                CardLongName = d?.Settings?.CardLongName,
                RecordingDeviceName = d?.Settings?.RecordingDeviceName,
                PlaybackDeviceName = d?.Settings?.PlaybackDeviceName,
                MixerDeviceName = d?.Settings?.MixerDeviceName,
                RecordingSampleRate = d?.Settings?.RecordingSampleRate,
                RecordingBitsPerSample = d?.Settings?.RecordingBitsPerSample,
                RecordingChannels = d?.Settings?.RecordingChannels
            }).ToList();

            summaryWriter.Append(new { Timestamp = DateTime.UtcNow, Devices = devicesList });
            _log.Info($"Wrote baseline summary for {_soundDevices.Count()} devices to {summaryPath}");
            foreach (var dev in devicesList)
            {
                _log.Info($"Discovered device: id={dev.CardId} name={dev.CardName} longname={dev.CardLongName} recording={dev.RecordingDeviceName} rate={dev.RecordingSampleRate} bits={dev.RecordingBitsPerSample} chans={dev.RecordingChannels}");
            }
        }
        catch (Exception ex)
        {
            _log.Warn($"Failed to write baseline summary: {ex.Message}");
        }

        foreach (var device in _soundDevices)
        {
            var settings = device?.Settings;
            var cardId = settings?.CardId ?? "unknown";
            var cardName = settings?.CardName ?? settings?.RecordingDeviceName ?? "unknown";
            var cardLong = settings?.CardLongName ?? string.Empty;
            var rate = settings?.RecordingSampleRate ?? 0u;
            var bits = settings?.RecordingBitsPerSample ?? 0;

            _log.Info($"Running baseline for device id={cardId} name={cardName} longname={cardLong} rate={rate} bits={bits}");

            // Log device info into JSON and logger
            var fileBase = SanitizeFileName(cardName ?? cardId ?? "unknown");
            var jsonPath = Path.Combine(measurementFolder, $"baseline_{_timestamp}_{fileBase}.json");
            var jsonWriter = new AlsaSharp.Library.Logging.JsonWriter(jsonPath);
            jsonWriter.Append(new { Device = device?.Settings, Card = new { Id = cardId, Name = cardName, LongName = cardLong, SampleRate = rate, BitsPerSample = bits }, Timestamp = DateTime.UtcNow });
            _log.Info($"Wrote device header to {jsonPath}");

            // Create per-device meter and recorder instances
            var meterLog = _services.GetRequiredService<ILog<AudioInterfaceLevelMeter>>();
            var recorderLog = _services.GetRequiredService<ILog<AudioLevelMeterRecorderService>>();
            var meter = new AudioInterfaceLevelMeter(device, meterLog);
            var recorder = new AudioLevelMeterRecorderService(recorderLog, meter, _recorderOptions);

                try
                {
                    var results = recorder.GetAudioMeterLevelReadings(_measureDuration, _measurementCount, jsonPath);
                    _log.Info($"Baseline for device {cardId} completed and written to {jsonPath}");
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"Failed to run baseline for device {cardId}");
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
