using AlsaSharp;
using AlsaSharp.Internal;
using Example.SNRReduction.Services;

namespace Example.SNRReduction.Interfaces;

public interface IControlSweepService
{
    public List<SNRSweepResult> SweepControl(ISoundDevice soundDevice, string mixerElementName, int controlMin, int controlMax, int controlStep, TimeSpan measurementDuration, int measurementCount);
}