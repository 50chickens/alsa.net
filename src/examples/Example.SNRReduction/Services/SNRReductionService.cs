using Example.SNRReduction.Interfaces;
using Example.SNRReduction.Models;
using AlsaSharp.Library.Logging;
using AlsaSharp;
using AlsaSharp.Internal;
using Example.SNRReduction;

namespace Example.SNRReduction.Services;

public class SignalNoiseRatioOptimizer(ILog<SignalNoiseRatioOptimizer> log, ControlSweepOptions controlSweepOptions, IAudioLevelMeterRecorderService audioLevelMeterRecorderService) : IControlSweepService
{
    private readonly ILog<SignalNoiseRatioOptimizer> _log = log;
    private ControlSweepOptions _controlSweepOptions = controlSweepOptions;
    private readonly IAudioLevelMeterRecorderService audioLevelMeterRecorderService = audioLevelMeterRecorderService;
    
    public List<ControlLevel> FindBestLevelsForControls(ControlSweepOptions options)
    {
           return new List<ControlLevel>()
           {
                new ControlLevel()
                {
                     ControlName = "Capture Volume",
                     ChannelName = "Front Left",
                     Value = 32768
                },
                new ControlLevel()
                {
                     ControlName = "Capture Volume",
                     ChannelName = "Front Right",
                     Value = 32768
                }
           };
    }

    // public List<SNRSweepResult> SweepControl(AudioCardMixerService probe, int cardIndex, string controlName, MixerControlInfo ch, SoundDeviceSettings soundSettings)
    // {
    //     var results = new List<SNRSweepResult>();
    //     nint min = ch.Min, max = ch.Max;
    //     long range = (long)(max - min);
    //     int steps = 5;
    //     long step = Math.Max(1, range / (steps - 1));

    //     var tools = new SNRTools(LogManager.GetLogger<SNRTools>());
    //     var resultsPath = Path.Combine(AppContext.BaseDirectory, "logs", "snr-sweep.jsonl");
    //     var writer = new ResultsWriter(resultsPath);

    //     for (long v = (long)min; v <= (long)max; v += step)
    //     {
    //         nint val = (nint)v;
    //         bool ok = probe.TrySetPlaybackVolume(cardIndex, controlName, ch.Name, val);
    //         if (!ok) ok = probe.TrySetCaptureVolume(cardIndex, controlName, ch.Name, val);
    //         if (!ok) continue;

    //         // short measurements
    //         using var dev1 = AlsaDeviceBuilder.Build(soundSettings);
    //         double noise = tools.MeasureNoise(dev1, 1);
    //         using var dev2 = AlsaDeviceBuilder.Build(soundSettings);
    //         double signal = tools.MeasureSignalAsync(dev2, 1, 1000).GetAwaiter().GetResult();
    //         double snr = noise <= 0 ? double.PositiveInfinity : 20.0 * Math.Log10(signal / noise);

    //         var res = new SNRSweepResult
    //         {
    //             ControlName = controlName,
    //             ChannelName = ch.Name,
    //             Value = (long)val,
    //             NoiseRms = noise,
    //             SignalRms = signal,
    //             SNRdB = snr
    //         };

    //         results.Add(res);
    //         try { writer.Append(res); } catch (Exception ex) { _log.Warn($"Failed to write sweep result: {ex.Message}"); }
    //     }

    //     return results;
    // }

}
