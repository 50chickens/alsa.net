using System.ComponentModel.DataAnnotations;

namespace Examples.SNRReduction.Models;

public class SNRReductionOptions
{
    public const string Settings = "SNRReduction";
    public string AudioCardName { get; set; } = string.Empty;
    public bool AutoSweep { get; set; } = false;
}