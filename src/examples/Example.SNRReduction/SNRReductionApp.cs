using System.Text.Json;
using AlsaSharp.Library.Logging;
using Example.SNRReduction.Audio;
using Example.SNRReduction.Models;
using Examples.SNRReduction.Interfaces;
using Examples.SNRReduction.Models;
using Examples.SNRReduction.Services;
namespace Example.SNRReduction;

public class SNRReductionApp(ILog<SNRReductionApp> log, ISNRReductionService snrReductionService, SNRReductionServiceOptions options, IAudioLevelMeterRecorderService audioLevelMeterRecorderService)
{
    private readonly ILog<SNRReductionApp> _log = log;
    private ISNRReductionService _snrReductionService = snrReductionService;
    private readonly IAudioLevelMeterRecorderService _audioLevelMeterRecorderService = audioLevelMeterRecorderService;
    private string fileNameToStoreMeasurements = "";
    private MeasurementResult _measurementResults = new MeasurementResult(DateTime.UtcNow, DateTime.UtcNow, TimeSpan.Zero, TimeSpan.Zero, string.Empty);
    public void Run()
    {
        //generate time stamped filename with a user defined prefix 
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _log.Info("Starting SNR Reduction Application");
        if (options.BaselineOnly)
        {
            _log.Info("BaselineOnly option set; skipping control sweep.");
            fileNameToStoreMeasurements = $"baseline_{timestamp}.json";
            _measurementResults.Readings = GetBaseLineReadings();
            new ResultsWriter(fileNameToStoreMeasurements).Append(_measurementResults);
            _log.Info($"Baseline measurements written to: {fileNameToStoreMeasurements}");
            return;
        }

        _log.Info("SNR Reduction Application Finished");

    }

    private List<AudioMeterLevelReading> GetBaseLineReadings()
    {
        var measurementResults = _audioLevelMeterRecorderService.GetAudioMeterLevelReadings(TimeSpan.FromSeconds(3), 10, "Baseline recording");
        measurementResults.ForEach(r =>
        {
            _log.Info($"Timestamp: {r.TimestampUtc}, Left: {r.LeftDbfs:F2} dBFS, Right: {r.RightDbfs:F2} dBFS");
        });
        return measurementResults;
    }

    private void SaveMeasurement(string fileName, List<AudioMeterLevelReading> measurementResults)
    {
        var opts = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(measurementResults, opts);
        File.WriteAllText(fileName, json);
        _log.Debug($"Baseline written to {fileName}");
    }
    // public void FindBestLevelsForControls(SNRReductionServiceOptions options)
    // {
    //     _log.Info($"Performing SNR Reduction using Audio Card: {options.AudioCardName}, AutoSweep: {options.AutoSweep}");


        
    //     _audioLevelMeterRecorderService.TakeAudioLevelRecording(TimeSpan.FromSeconds(3));
    //     // Run an extended baseline: 1 minute total, readings every 3 seconds.
    //     var baselineFile = AudioLevelMeterRecorderService.RecordMeasurementsToFile(device, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(3), "Baseline input levels - no playback signal", _log);
    //     _log.Info($"Baseline measurements written to: {baselineFile}");

    //     // Baseline signal/noise for SNR calculations
    //     var tools = new SNRTools(AlsaSharp.Library.Logging.LogManager.GetLogger<SNRTools>());
    //     double baselineSignal = tools.MeasureSignalAsync(device, 2, 1000).GetAwaiter().GetResult();
    //     double baselineNoise = tools.MeasureNoise(device, 2);
    //     double baselineSNRdB = baselineNoise <= 0 ? double.PositiveInfinity : 20.0 * Math.Log10(baselineSignal / baselineNoise);
    //     _log.Info($"Baseline combined signal RMS={baselineSignal:F6}, noise RMS={baselineNoise:F6}, SNR={baselineSNRdB:F2} dB");

    //     // Find mixer card index (try to match by name)
    //     int cardIndex = GetCardIndex(options.AudioCardName);
    //     if (cardIndex < 0)
    //     {
    //         _log.Warn("No ALSA cards found; skipping sweep.");
    //         return;
    //     }

    //     var probe = new MixerProbe();
    //     var controls = probe.GetControlsForCard(cardIndex);
    //     foreach (var c in controls)
    //     {
    //         foreach (var ch in c.Channels)
    //         {
    //             // skip channels with no range
    //             if (ch.Max <= ch.Min) continue;
    //             // sample a few points across the range
    //             var results = SweepControl(probe, cardIndex, c.ControlName, ch, soundSettings);
    //             // pick best by SNR while keeping signal within ~1 dB of baseline
    //             var acceptable = results.Where(r => Math.Abs(20.0 * Math.Log10(r.SignalRms / baselineSignal)) <= 1.0).ToList();
    //             var best = acceptable.OrderByDescending(r => r.SNRdB).FirstOrDefault();
    //             if (best != null)
    //             {
    //                 _log.Info($"Control={c.ControlName} Channel={ch.Name} -> best value={best.Value} SNR={best.SNRdB:F2} dB");
    //             }
    //         }
    //     }
    // }
}