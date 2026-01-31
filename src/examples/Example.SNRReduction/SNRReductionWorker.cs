using AlsaSharp;
using AlsaSharp.Library.Builders;
using AlsaSharp.Library.Logging;
using AlsaSharp.Library.Services;
using Example.SNRReduction.Models;
using Example.SNRReduction.Services;

using Microsoft.Extensions.Options;

namespace Example.SNRReduction;

/// <summary>
/// Hosted worker that runs the baseline measurement and prints results to the console.
/// Determines test type from command-line arguments and dispatches to appropriate service.
/// </summary>
public class SNRReductionWorker(ILog<SNRReductionWorker> log,
    IOptions<SNRReductionServiceOptions> options,
    IHostApplicationLifetime lifetime,
    IAudioDeviceBuilder audioDeviceBuilder,
    IAudioCardSelector cardSelector,
    IAudioLevelMeterRecorderService audioLevelMeterRecorderService,
    ITestToneService testToneService,
    IAlsaLoopbackTestService loopbackTestService,
    ISNRMeasurementService snrMeasurementService,
    ICopyOnlyService copyOnlyService,
    string[] args) : BackgroundService
{
    private readonly ILog<SNRReductionWorker> _log = log ?? throw new ArgumentNullException(nameof(log));
    private readonly SNRReductionServiceOptions _snrReductionServiceOptions = options?.Value ?? new SNRReductionServiceOptions();
    private IEnumerable<ISoundDevice> _soundDevices = Enumerable.Empty<ISoundDevice>();
    private readonly IHostApplicationLifetime _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
    private readonly IAudioDeviceBuilder _audioDeviceBuilder = audioDeviceBuilder ?? throw new ArgumentNullException(nameof(audioDeviceBuilder));
    private readonly IAudioCardSelector _cardSelector = cardSelector ?? throw new ArgumentNullException(nameof(cardSelector));
    private readonly IAudioLevelMeterRecorderService _audioLevelMeterRecorderService = audioLevelMeterRecorderService ?? throw new ArgumentNullException(nameof(audioLevelMeterRecorderService));
    private readonly ITestToneService _testToneService = testToneService ?? throw new ArgumentNullException(nameof(testToneService));
    private readonly IAlsaLoopbackTestService _loopbackTestService = loopbackTestService ?? throw new ArgumentNullException(nameof(loopbackTestService));
    private readonly ISNRMeasurementService _snrMeasurementService = snrMeasurementService ?? throw new ArgumentNullException(nameof(snrMeasurementService));
    private readonly ICopyOnlyService _copyOnlyService = copyOnlyService ?? throw new ArgumentNullException(nameof(copyOnlyService));
    private readonly string[] _args = args ?? Array.Empty<string>();
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _log.Trace("SNRReductionWorker starting...");

            // Determine test type from command-line arguments
            var testType = DetermineTestType(_args);

            // Dispatch to appropriate service based on test type
            switch (testType)
            {
                case SNRTestType.GenerateTestTone:
                    await ExecuteGenerateTestToneAsync(stoppingToken);
                    break;

                case SNRTestType.MeasureSNR:
                    await ExecuteMeasureSNRAsync(stoppingToken);
                    break;

                case SNRTestType.MeasureAudioLevels:
                    await ExecuteMeasureAudioLevelsAsync(stoppingToken);
                    break;

                case SNRTestType.TestLoopback:
                    await ExecuteTestLoopbackAsync(stoppingToken);
                    break;
                case SNRTestType.CopyOnly:
                    await ExecuteCopyOnlyAsync(stoppingToken);
                    break;
                case SNRTestType.None:
                default:
                    _log.Info("No specific test type determined. Running default audio level measurement...");
                    await ExecuteMeasureAudioLevelsAsync(stoppingToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error during SNR reduction worker execution");
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }

    /// <summary>
    /// Determines the SNR test type from command-line arguments.
    /// Uses pattern matching on argument strings (--flag format).
    /// </summary>
    private SNRTestType DetermineTestType(string[] args)
    {
        var argString = string.Join(" ", args);

        return argString switch
        {
            _ when argString.Contains("--test-tone") => SNRTestType.GenerateTestTone,
            _ when argString.Contains("--measure-snr") => SNRTestType.MeasureSNR,
            _ when argString.Contains("--measure-levels") => SNRTestType.MeasureAudioLevels,
            _ when CommandLineParser.HasArgument(args, "test-loopback") => SNRTestType.TestLoopback,
            _ when argString.Contains("--copy-only") => SNRTestType.CopyOnly,
            _ => SNRTestType.None
        };
    }

    /// <summary>
    /// Executes the test tone generation on all audio devices.
    /// </summary>
    private async Task ExecuteGenerateTestToneAsync(CancellationToken stoppingToken)
    {
        _log.Info("=== Generating Test Tone ===");
        _soundDevices = _audioDeviceBuilder.BuildAudioDevices();
        
        foreach (ISoundDevice device in _soundDevices)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            _log.Info($"Playing test tone on sound device: {device.Settings.CardName}");
            _testToneService.PlayTestTone(
                device.Settings.PlaybackDeviceName,
                _snrReductionServiceOptions.TargetFrequencyHz,
                _snrReductionServiceOptions.TestToneAmplitudeDbfs,
                _snrReductionServiceOptions.TestToneLeftChannelDuration,
                _snrReductionServiceOptions.TestToneRightChannelDuration,
                _snrReductionServiceOptions.TestToneBothChannelsDuration
            );
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Executes SNR measurement on all audio devices.
    /// </summary>
    private async Task ExecuteMeasureSNRAsync(CancellationToken stoppingToken)
    {
        _log.Info("=== Measuring SNR ===");
        _soundDevices = _audioDeviceBuilder.BuildAudioDevices();
        
        foreach (ISoundDevice device in _soundDevices)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            _log.Info($"Measuring SNR for sound device: {device.Settings.CardName}");
            _snrMeasurementService.MeasureSNR(device, _snrReductionServiceOptions.TargetFrequencyHz, stoppingToken);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Executes audio level measurement and recording on all audio devices.
    /// </summary>
    private async Task ExecuteMeasureAudioLevelsAsync(CancellationToken stoppingToken)
    {
        _log.Info("=== Measuring Audio Levels ===");
        _soundDevices = _audioDeviceBuilder.BuildAudioDevices();
        
        foreach (ISoundDevice device in _soundDevices)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            _log.Info($"Recording levels for sound device: {device.Settings.CardName}");
            _audioLevelMeterRecorderService.RecordAudioMeterLevels(device, stoppingToken);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Executes loopback test on all audio devices.
    /// </summary>
    private async Task ExecuteTestLoopbackAsync(CancellationToken stoppingToken)
    {
        _log.Info("=== ALSA Loopback Test Mode ===");
        
        var allDevices = _audioDeviceBuilder.BuildAudioDevices();
        var cardSelector = CommandLineParser.GetArgumentValue(_args, "loopback-test-card") 
                          ?? CommandLineParser.GetArgumentValue(_args, "test-loopback");
        
        _soundDevices = string.IsNullOrWhiteSpace(cardSelector) 
            ? allDevices 
            : _cardSelector.SelectCards(allDevices, cardSelector);

        var deviceList = _soundDevices.ToList();
        
        if (!deviceList.Any())
        {
            _log.Error("No audio devices found matching the specified criteria");
            return;
        }

        _log.Info($"Testing {deviceList.Count} device(s)");
        
        foreach (var device in deviceList)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            _log.Info($"Testing card #{deviceList.IndexOf(device)}: {device.Settings.CardName}");
            
            try
            {
                var (playedSignal, recordedSignal, isWorking) = _loopbackTestService.TestLoopback(device, device, 3000);
                _log.Info($"Result: {(isWorking ? "SUCCESS" : "FAILED")}");
            }
            catch (Exception ex)
            {
                _log.Error($"Loopback test failed for {device.Settings.CardName}: {ex.Message}");
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Executes copy-only test on all audio devices.
    /// Copies incoming audio from input channels 1/2 to output channels 1/2.
    /// </summary>
    private async Task ExecuteCopyOnlyAsync(CancellationToken stoppingToken)
    {
        _log.Info("=== Copy-Only Test Mode ===");
        _soundDevices = _audioDeviceBuilder.BuildAudioDevices();
        
        foreach (ISoundDevice device in _soundDevices)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            _log.Info($"Executing copy-only test on device: {device.Settings.CardName}");
            try
            {
                _copyOnlyService.CopyAudioChannels(device, 50000, stoppingToken);
                _log.Info("Copy-only test completed successfully");
            }
            catch (Exception ex)
            {
                _log.Error($"Copy-only test failed: {ex.Message}");
            }
        }

        await Task.CompletedTask;
    }
    
}
