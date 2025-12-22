using AlsaSharp;

namespace Example.SNRReduction.Services;

public interface IAudioInterfaceLevelMeterService
{
    (List<double> ChannelDbfs, List<double> ChannelRms) MeasureLevels(ISoundDevice device, int captureDurationMs, CancellationToken cancellationToken);
}
