using AlsaSharp;
using Example.SNRReduction.Models;

namespace Example.SNRReduction.Services;

public interface IAudioLevelMeterRecorderService
{
    List<AudioMeterLevelReading> RecordAudioMeterLevels(ISoundDevice device);
}
