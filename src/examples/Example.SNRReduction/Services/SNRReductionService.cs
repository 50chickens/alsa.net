using Examples.SNRReduction.Interfaces;
using Examples.SNRReduction.Models;

namespace Examples.SNRReduction.Services;

public class SNRReductionService(ILogger<SNRReductionService> logger, SNRReductionOptions options) : ISNRReductionService
{
    private readonly ILogger<SNRReductionService> _logger = logger;
    private readonly SNRReductionOptions _options = options;

    public void FindBestLevelsForControls(SNRReductionOptions options)
    {
        _logger.LogInformation("Performing SNR Reduction using Audio Card: {AudioCardName}, AutoSweep: {AutoSweep}", options.AudioCardName, options.AutoSweep);
        // Implementation of SNR reduction logic goes here.
    }

}
