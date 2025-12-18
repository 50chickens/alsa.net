using Example.SNRReduction.Models;
using Microsoft.Extensions.Options;
#nullable enable

namespace Example.SNRReduction.Services;

public class SNRReductionOptionsValidationService : IValidateOptions<SNRReductionServiceOptions>
{
    
    public ValidateOptionsResult Validate(string? name, SNRReductionServiceOptions options)
    {
        // var failures = new List<string>();

        // if (string.IsNullOrEmpty(options.AudioCardName))
        // {
        //     failures.Add("AudioCardName must be provided.");
        // }

        // if (failures.Count > 0)
        // {
        //     return ValidateOptionsResult.Fail(failures);
        // }

        return ValidateOptionsResult.Success;
    }
}