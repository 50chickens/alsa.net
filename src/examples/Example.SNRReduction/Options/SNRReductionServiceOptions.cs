namespace Example.SNRReduction.Models;

public class SNRReductionServiceOptions
{
    public const string Settings = "SNRReduction";
    public bool AutoSweep { get; set; } = false;
    public bool BaselineOnly { get; set; } = true;
    public bool ApplyAlsaStateFile { get; set; } = false;
    public string DefaultAudioStateFolderName { get; set; } = "~/pi-stomp/setup/audio";
    /// <summary>
    /// When true the worker will attempt to programmatically set a sane DAI Left Source MUX
    /// (e.g. try ADC Right/ADC Left) before taking measurements. Default is false.
    /// </summary>
    public bool AutoConfigureDaiMux { get; set; } = false;
    public bool MeasureSNR { get; set; } = false;
    public bool MeasureAudioLevels { get; set; } = true;
    public string ApplyAlsaState { get; internal set; } = string.Empty;
    public bool GenerateTestTone { get; set; } = false;
    public int TestToneFrequencyHz { get; set; } = 440;
    public double TestToneAmplitudeDbfs { get; set; } = -3.0;
    public int TestToneLeftChannelDuration { get; set; } = 5000;
    public int TestToneRightChannelDuration { get; set; } = 5000;
    public int TestToneSilenceDuration { get; set; } = 2000;
    public int TestToneBothChannelsDuration { get; set; } = 5000;
    public bool VerifyLoopBack { get; set; } = true;
}
