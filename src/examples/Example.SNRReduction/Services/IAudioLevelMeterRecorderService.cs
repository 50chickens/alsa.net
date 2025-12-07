using Example.SNRReduction.Audio;
using Example.SNRReduction.Models;

namespace Example.SNRReduction.Services;

public interface IAudioLevelMeterRecorderService
{
    public List<AudioMeterLevelReading> GetAudioMeterLevelReadings(TimeSpan measurementDuration, int measurementCount, string description);
}