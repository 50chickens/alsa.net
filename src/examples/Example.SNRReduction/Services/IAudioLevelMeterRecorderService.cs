using Example.SNRReduction.Audio;
using Example.SNRReduction.Models;

namespace Example.SNRReduction.Services;

public interface IAudioLevelMeterRecorderService
{
    List<AudioMeterLevelReading> GetAudioMeterLevelReadings(TimeSpan measurementDuration, int measurementCount, string resultFileName);
    AudioMeterLevelReading GetAudioMeterLevelReading(TimeSpan measurementDuration);
}