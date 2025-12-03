using Examples.SNRReduction.Models;
using Microsoft.Extensions.Options;

namespace Examples.SNRReduction.Services;

public class SNRReductionOptionsValidationService : IValidateOptions<SNRReductionOptions>
{
    
    public ValidateOptionsResult Validate(string name, SNRReductionOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrEmpty(options.AudioCardName))
        {
            failures.Add("AudioCardName must be provided.");
        }

        if (failures.Count > 0)
        {
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }
}