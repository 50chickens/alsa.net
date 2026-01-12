namespace Example.SNRReduction.Models;

/// <summary>
/// Defines the types of SNR reduction tests that can be executed.
/// </summary>
public enum SNRTestType
{
    /// <summary>
    /// No specific test - display test options or exit.
    /// </summary>
    None = 0,

    /// <summary>
    /// Measure SNR (Signal-to-Noise Ratio).
    /// </summary>
    MeasureSNR = 1,

    /// <summary>
    /// Generate a test tone.
    /// </summary>
    GenerateTestTone = 2,

    /// <summary>
    /// Measure audio levels.
    /// </summary>
    MeasureAudioLevels = 3,

    /// <summary>
    /// Test loopback functionality.
    /// </summary>
    TestLoopback = 4
}
