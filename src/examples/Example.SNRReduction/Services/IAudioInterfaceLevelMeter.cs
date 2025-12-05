namespace Example.SNRReduction.Services;

public interface IAudioInterfaceLevelMeter
{
    (double LeftDbfs, double RightDbfs) MeasureLevels(int captureDurationMs);
}