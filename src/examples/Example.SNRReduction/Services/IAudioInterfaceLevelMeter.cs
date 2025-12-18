namespace Example.SNRReduction.Services;

public interface IAudioInterfaceLevelMeter
{
    // Returns a tuple of per-channel dBFS values and per-channel RMS values.
    (List<double> ChannelDbfs, List<double> ChannelRms) MeasureLevels(int captureDurationMs);
}
