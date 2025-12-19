using Example.SNRReduction.Audio;
using Example.SNRReduction.Models;

namespace Example.SNRReduction.Services;

public interface IAudioLevelMeterRecorderService
{
    List<AudioMeterLevelReading> GetAudioMeterLevelReadings(Example.SNRReduction.Services.IAudioInterfaceLevelMeter meter, TimeSpan measurementDuration, int measurementCount, string resultFileName);
    AudioMeterLevelReading GetAudioMeterLevelReading(Example.SNRReduction.Services.IAudioInterfaceLevelMeter meter, TimeSpan measurementDuration);
    // Back-compat convenience methods retained for callers that do not have a per-device
    // meter object; these are optional and may throw if a meter is required.
    List<AudioMeterLevelReading> GetAudioMeterLevelReadings(TimeSpan measurementDuration, int measurementCount, string resultFileName);
    AudioMeterLevelReading GetAudioMeterLevelReading(TimeSpan measurementDuration);
}
