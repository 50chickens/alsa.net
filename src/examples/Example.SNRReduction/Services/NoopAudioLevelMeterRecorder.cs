using Example.SNRReduction.Models;

namespace Example.SNRReduction.Services;

/// <summary>
/// Minimal noop implementation of <see cref="IAudioLevelMeterRecorderService"/> used for DI bootstrapping.
/// Returns NaN readings so consuming code can handle missing real hardware.
/// </summary>
public class NoopAudioLevelMeterRecorder : IAudioLevelMeterRecorderService
{
    public List<AudioMeterLevelReading> GetAudioMeterLevelReadings(TimeSpan measurementDuration, int measurementCount, string resultFileName)
    {
        var list = new List<AudioMeterLevelReading>();
        for (int i = 0; i < measurementCount; i++)
        {
            list.Add(new AudioMeterLevelReading
            {
                TimestampUtc = DateTime.UtcNow,
                LeftDbfs = double.NaN,
                RightDbfs = double.NaN,
                LeftRms = double.NaN,
                RightRms = double.NaN
            });
        }
        return list;
    }

    public AudioMeterLevelReading GetAudioMeterLevelReading(TimeSpan measurementDuration)
    {
        return new AudioMeterLevelReading
        {
            TimestampUtc = DateTime.UtcNow,
            LeftDbfs = double.NaN,
            RightDbfs = double.NaN,
            LeftRms = double.NaN,
            RightRms = double.NaN
        };
    }
}
