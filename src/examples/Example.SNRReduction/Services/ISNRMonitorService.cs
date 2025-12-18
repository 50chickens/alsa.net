using AlsaSharp;

namespace Example.SNRReduction.Services;

public interface ISNRMonitorService
{
    Task RunContinuousMonitoringAsync(ISoundDevice device, TimeSpan measureDuration, int samples, string measurementFolder, CancellationToken token);
}
