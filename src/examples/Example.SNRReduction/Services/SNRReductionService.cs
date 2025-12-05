using Examples.SNRReduction.Interfaces;
using Examples.SNRReduction.Models;
using AlsaSharp.Library.Logging;
using AlsaSharp;
using AlsaSharp.Internal;
using Example.SNRReduction;

namespace Examples.SNRReduction.Services;

public class SNRReductionService(ILog<SNRReductionService> log, SNRReductionServiceOptions snrReductionOptions, IAudioLevelMeterRecorderService audioLevelMeterRecorderService) : ISNRReductionService
{
    private readonly ILog<SNRReductionService> _log = log;
    private SNRReductionServiceOptions _snrReductionOptions = snrReductionOptions;
    private readonly IAudioLevelMeterRecorderService audioLevelMeterRecorderService = audioLevelMeterRecorderService;
    
    public void FindBestLevelsForControls(SNRReductionServiceOptions options)
    {
           
    }
    
    
    private int GetCardIndex(string? audioCardName)
    {
        try
        {
            var enumerator = new AlsaCardEnumerator();
            var cards = enumerator.GetCards().ToList();
            if (cards.Count == 0) return -1;
            if (string.IsNullOrEmpty(audioCardName)) return cards[0].Index;
            var match = cards.FirstOrDefault(c => string.Equals(c.Name, audioCardName, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match.Index;
            // fallback: try contains
            match = cards.FirstOrDefault(c => c.Name?.IndexOf(audioCardName ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0);
            return match != null ? match.Index : cards[0].Index;
        }
        catch (Exception ex)
        {
            _log.Warn($"Error enumerating cards: {ex.Message}");
            return -1;
        }
    }


    private List<SNRSweepResult> SweepControl(MixerProbe probe, int cardIndex, string controlName, AlsaSharp.Internal.MixerControlChannelInfo ch, SoundDeviceSettings soundSettings)
    {
        var results = new List<SNRSweepResult>();
        nint min = ch.Min, max = ch.Max;
        long range = (long)(max - min);
        int steps = 5;
        long step = Math.Max(1, range / (steps - 1));

        var tools = new SNRTools(AlsaSharp.Library.Logging.LogManager.GetLogger<SNRTools>());
        var resultsPath = Path.Combine(AppContext.BaseDirectory, "logs", "snr-sweep.jsonl");
        var writer = new ResultsWriter(resultsPath);

        for (long v = (long)min; v <= (long)max; v += step)
        {
            nint val = (nint)v;
            bool ok = probe.TrySetPlaybackVolume(cardIndex, controlName, ch.Name, val);
            if (!ok) ok = probe.TrySetCaptureVolume(cardIndex, controlName, ch.Name, val);
            if (!ok) continue;

            // short measurements
            using var dev1 = AlsaDeviceBuilder.Create(soundSettings);
            double noise = tools.MeasureNoise(dev1, 1);
            using var dev2 = AlsaDeviceBuilder.Create(soundSettings);
            double signal = tools.MeasureSignalAsync(dev2, 1, 1000).GetAwaiter().GetResult();
            double snr = noise <= 0 ? double.PositiveInfinity : 20.0 * Math.Log10(signal / noise);

            var res = new SNRSweepResult
            {
                ControlName = controlName,
                ChannelName = ch.Name,
                Value = (long)val,
                NoiseRms = noise,
                SignalRms = signal,
                SNRdB = snr
            };

            results.Add(res);
            try { writer.Append(res); } catch (Exception ex) { _log.Warn($"Failed to write sweep result: {ex.Message}"); }
        }

        return results;
    }

}
