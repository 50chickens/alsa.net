using Examples.SNRReduction.Interfaces;
using Examples.SNRReduction.Models;
namespace Example.SNRReduction;

public class SNRReductionApp(ILogger<SNRReductionApp> logger, ISNRReductionService snrReductionService, SNRReductionOptions snrReductionOptions)
{
    private readonly ILogger<SNRReductionApp> _logger = logger;
    private ISNRReductionService _snrReductionService = snrReductionService;
    private SNRReductionOptions _snrReductionOptions { get; } = snrReductionOptions;
    public void GetSNRReduction()
    {
        _logger.LogInformation("Starting SNR Reduction Application");

        _snrReductionService.FindBestLevelsForControls(_snrReductionOptions);

        _logger.LogInformation("SNR Reduction Application Finished");
        
        
    }
}