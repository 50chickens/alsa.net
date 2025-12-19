using AlsaSharp.Library.Logging;
namespace Example.SNRReduction;

public class SNRReductionApp(ILog<SNRReductionApp> log)
{
    private readonly ILog<SNRReductionApp> _log = log;
    
    public void Run()
    {
        _log.Info("SNR Reduction Application Finished");
    }

}
