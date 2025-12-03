using Examples.SNRReduction.Interfaces;
using Examples.SNRReduction.Models;
using AlsaSharp.Library.Logging;

namespace Examples.SNRReduction.Services;

public class SNRReductionService(ILog<SNRReductionService> logger, SNRReductionOptions options) : ISNRReductionService
{
    private readonly ILog<SNRReductionService> _logger = logger;
    private readonly SNRReductionOptions _options = options;

    public void FindBestLevelsForControls(SNRReductionOptions options)
    {
        _logger.Info($"Performing SNR Reduction using Audio Card: {options.AudioCardName}, AutoSweep: {options.AutoSweep}");
        // Implementation of SNR reduction logic goes here.
    }

}
