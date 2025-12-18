using AlsaSharp;
namespace Example.SNRReduction.Services;

public interface ILoopbackTester
{
    Task RunLoopbackTestAsync(ISoundDevice device, CancellationToken token);
}
