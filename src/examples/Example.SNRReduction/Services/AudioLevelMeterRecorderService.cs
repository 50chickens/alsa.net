using AlsaSharp.Library.Logging;
using Example.SNRReduction.Models;

namespace Example.SNRReduction.Services;
public class AudioLevelMeterRecorderService(ILog<AudioLevelMeterRecorderService> log, IAudioInterfaceLevelMeter audioInterfaceLevelMeter, AudioLevelMeterRecorderServiceOptions options) : IAudioLevelMeterRecorderService
{
    private readonly ILog<AudioLevelMeterRecorderService> _log = log;
    private readonly AudioLevelMeterRecorderServiceOptions _options = options;
    private readonly IAudioInterfaceLevelMeter _audioInterfaceLevelMeter = audioInterfaceLevelMeter;
    
    
    public List<AudioMeterLevelReading> GetAudioMeterLevelReadings(TimeSpan measurementDuration, int measurementCount, string resultFileName)
    {
        if (measurementDuration <= TimeSpan.Zero) throw new ArgumentException("measureFor must be > 0", nameof(measurementDuration));
        if (measurementCount <= 0) throw new ArgumentException("measurementCount must be > 0", nameof(measurementCount));
        List<AudioMeterLevelReading> _audioLevelReadings = new();
        
        for (int i = 0; i < measurementCount; i++)
        {
            _log.Info($"Starting measurement {i + 1} of {measurementCount} for duration {measurementDuration.TotalSeconds} seconds...");
            AudioMeterLevelReading audioMeterLevelReading = GetAudioMeterLevelReading(measurementDuration);
            _log.Info($"Baseline reading: L={audioMeterLevelReading.LeftDbfs:F2} dBFS R={audioMeterLevelReading.RightDbfs:F2} dBFS");
            JsonWriter jsonWriter = new JsonWriter(resultFileName);
            jsonWriter.Append(audioMeterLevelReading);
            _audioLevelReadings.Add(audioMeterLevelReading);
            _log.Info($"Completed measurement {i + 1} of {measurementCount} for duration {measurementDuration.TotalSeconds} seconds.");
        }
        return _audioLevelReadings;
    }
    public AudioMeterLevelReading GetAudioMeterLevelReading(TimeSpan measurementDuration)
    {
        var (leftDbfs, rightDbfs) = _audioInterfaceLevelMeter.MeasureLevels((int)measurementDuration.TotalMilliseconds);
                double leftRms = double.IsNegativeInfinity(leftDbfs) ? 0.0 : Math.Pow(10.0, leftDbfs / 20.0);
                double rightRms = double.IsNegativeInfinity(rightDbfs) ? 0.0 : Math.Pow(10.0, rightDbfs / 20.0);
            AudioMeterLevelReading audioLevelReading = new()
            {
                TimestampUtc = DateTime.UtcNow,
                LeftDbfs = leftDbfs,
                RightDbfs = rightDbfs,
                LeftRms = leftRms,
                RightRms = rightRms
            };
            return audioLevelReading;
    }
}
