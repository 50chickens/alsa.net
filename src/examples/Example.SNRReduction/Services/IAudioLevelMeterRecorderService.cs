using Example.SNRReduction.Audio;
using Example.SNRReduction.Models;

namespace Examples.SNRReduction.Services;

public interface IAudioLevelMeterRecorderService
{
    public List<AudioMeterLevelReading> GetAudioMeterLevelReadings(TimeSpan measurementDuration, int measurementCount, string description);
}