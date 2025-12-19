using AlsaSharp;
using Example.SNRReduction.Services;

namespace Example.SNRReduction.Interfaces;

public interface IControlSweepService
{
    public List<SNRSweepResult> SweepControl(ISoundDevice soundDevice, string mixerElementName, int controlMin, int controlMax, int controlStep, TimeSpan measurementDuration, int measurementCount, IAudioLevelMeterRecorderService? recorder = null, Example.SNRReduction.Services.IAudioInterfaceLevelMeter? meter = null);
}
