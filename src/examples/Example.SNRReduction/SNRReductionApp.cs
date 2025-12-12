using AlsaSharp.Library.Logging;
using Example.SNRReduction.Models;
using Example.SNRReduction.Interfaces;
using Example.SNRReduction.Services;
namespace Example.SNRReduction;

public class SNRReductionApp(ILog<SNRReductionApp> log, IControlSweepService controlSweepService, SNRReductionServiceOptions options, IAudioLevelMeterRecorderService audioLevelMeterRecorderService)
{
    private readonly ILog<SNRReductionApp> _log = log;
    private IControlSweepService _controlSweepService = controlSweepService;
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
            new JsonWriter(fileNameToStoreMeasurements).Append(_measurementResults);
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
}