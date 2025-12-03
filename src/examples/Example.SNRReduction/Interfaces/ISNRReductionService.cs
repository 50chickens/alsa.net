using Examples.SNRReduction.Models;

namespace Examples.SNRReduction.Interfaces;

public interface ISNRReductionService
{
    void FindBestLevelsForControls(SNRReductionOptions options);
}