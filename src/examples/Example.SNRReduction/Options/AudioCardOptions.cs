using System;

namespace Example.SNRReduction.Models;

public class AudioCardOptions
{
    public const string Settings = "AudioLevelMeterRecorderService";
    public string AudioCardName { get; set; } = "IQAudio";
}
