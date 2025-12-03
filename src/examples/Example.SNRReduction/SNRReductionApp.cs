using Examples.SNRReduction.Interfaces;
using Examples.SNRReduction.Models;
using Example.SNRReduction.Logging;
namespace Example.SNRReduction;

public class SNRReductionApp(ILog<SNRReductionApp> logger, ISNRReductionService snrReductionService, SNRReductionOptions snrReductionOptions)
{
    private readonly ILog<SNRReductionApp> _logger = logger;
    private ISNRReductionService _snrReductionService = snrReductionService;
    private SNRReductionOptions _snrReductionOptions { get; } = snrReductionOptions;
    public void GetSNRReduction()
    {
        _logger.Info("Starting SNR Reduction Application");
        _snrReductionService.FindBestLevelsForControls(_snrReductionOptions);
        _logger.Info("SNR Reduction Application Finished");
    }
}