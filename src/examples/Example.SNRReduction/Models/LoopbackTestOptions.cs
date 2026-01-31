#nullable enable

namespace Example.SNRReduction.Models;

public class LoopbackTestOptions
{
    public const string Settings = "LoopbackTest";
    
    public double TestLevelDbfs { get; set; } = -12.0;
    public int TestDurationMs { get; set; } = 3000;
    public string? CardSelector { get; set; }
    public string ResultsFolder { get; set; } = "results";
}
