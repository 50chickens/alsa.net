using AlsaSharp;
using AlsaSharp.Library.Builders;
using AlsaSharp.Library.Logging;
using Example.SNRReduction.Models;
using Example.SNRReduction.Services;

using Microsoft.Extensions.Options;

namespace Example.SNRReduction;

/// <summary>
/// Hosted worker that runs the baseline measurement and prints results to the console.
/// </summary>
public class SNRReductionWorker(ILog<SNRReductionWorker> log,
    IOptions<SNRReductionServiceOptions> options,
    IHostApplicationLifetime lifetime,
    IAudioDeviceBuilder audioDeviceBuilder,
    IAudioLevelMeterRecorderService audioLevelMeterRecorderService) : BackgroundService
{
    private readonly ILog<SNRReductionWorker> _log = log ?? throw new ArgumentNullException(nameof(log));
    private readonly SNRReductionServiceOptions _snrReductionServiceOptions = options?.Value ?? new SNRReductionServiceOptions();
    private IEnumerable<ISoundDevice> _soundDevices = Enumerable.Empty<ISoundDevice>();
    private readonly IHostApplicationLifetime _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
    private readonly IAudioDeviceBuilder _audioDeviceBuilder = audioDeviceBuilder ?? throw new ArgumentNullException(nameof(audioDeviceBuilder));
    private readonly IAudioLevelMeterRecorderService _audioLevelMeterRecorderService = audioLevelMeterRecorderService ?? throw new ArgumentNullException(nameof(audioLevelMeterRecorderService));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.Trace("SNRReductionWorker starting baseline measurement...");


        if (_snrReductionServiceOptions.MeasureAudioLevels)
        {

            _soundDevices = _audioDeviceBuilder.BuildAudioDevices();
            foreach (ISoundDevice device in _soundDevices)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;
                _log.Info($"Recording levels for sound device: {device.Settings.CardName}");
                _audioLevelMeterRecorderService.RecordAudioMeterLevels(device, stoppingToken);
            }
        }
        else
        {
            _log.Info("MeasureAudioLevels is false. Not doing audio level baseline recording.");
        }
        _lifetime.StopApplication();
    }
    
}
