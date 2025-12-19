namespace Example.SNRReduction.Models;

public class SNRAnalysisResult
{
    /// AverageSnrDb represents the average signal-to-noise ratio (SNR) in decibels (dB) across the entire audio sample.
    /// it is measured by comparing the level of the desired signal to the level of background noise present in the audio sample.
    /// A higher SNR value indicates a cleaner audio signal with less noise interference.
    /// a good value is generally considered to be above 20 dB, indicating that the signal is significantly stronger than the noise.
    /// a poor value is typically below 10 dB, suggesting that the noise level is relatively high compared to the signal.
    /// a value of 2db or lower indicates very noisy audio with the signal barely distinguishable from the noise.
    public double AverageSnrDb { get; set; }
    /// NoiseSections represents the number of sections in the audio sample that are considered noisy.
    public int CleanSections { get; set; }
    /// CleanSections represents the number of sections in the audio sample that are considered clean (i.e., with low noise).
    /// it is measured by dividing the audio sample into smaller segments (sections) and evaluating the noise level in each segment.
    /// A section is classified as clean if its noise level falls below a predefined threshold, indicating that the audio signal in that section is relatively free from unwanted noise.    

    public int NoiseSections { get; set; }
    /// SectionSnrDb is a list containing the SNR values in decibels (dB) for each individual section of the audio sample.
    /// it is measured by dividing the audio sample into smaller segments (sections) and calculating the SNR for each segment separately.
    /// This allows for a more detailed analysis of the audio quality over time, as different sections may have varying levels of noise and signal strength.
    public List<double> SectionSnrDb { get; set; } = new List<double>();

    /// Frames represents the total number of audio frames in the sample.
    public int Frames { get; set; }
    /// Channels represents the number of audio channels in the sample.
    public int Channels { get; set; }
    /// BytesPerSample represents the number of bytes per audio sample.
    public int BytesPerSample { get; set; }
    /// SampleRate represents the audio sample rate in hertz (Hz).
    public int SampleRate { get; set; }
    /// AverageSignalDb represents the average signal level in decibels (dB).
    public double AverageSignalDb { get; set; }
    /// AverageNoiseDb represents the average noise level in decibels (dB).
    /// it indicates the average level of unwanted background noise present in the audio signal.
    /// it is measured by analyzing the portions of the audio signal that do not contain the desired signal (e.g., silence or low-level noise segments).
    /// a low average noise level has a db level of -60 dB or lower, indicating a quiet background with minimal interference.
    /// a high average noise level has a db level of -30 dB or higher, indicating significant background noise that may interfere with the desired audio signal.
    public double AverageNoiseDb { get; set; }
    /// AverageOutputDb represents the average output level in decibels (dB).
    public double AverageOutputDb { get; set; }
    /// TotalHarmonicDistortionDb represents the total harmonic distortion in decibels (dB). 
    /// it means that the distortion level is measured relative to the fundamental frequency of the audio signal. 
    /// It is measured by comparing the power of the harmonic distortion components to the power of the fundamental frequency component.   
    /// A higher THD value indicates more distortion in the audio signal.
    /// values of THD are typically expressed in decibels (dB) to provide a logarithmic scale for easier interpretation.
    /// a good THD value for audio equipment is generally considered to be below 1% (or -40 dB), with lower values indicating better audio quality.
    /// However, acceptable THD levels can vary depending on the specific application and listener preferences.

    public double TotalHarmonicDistortionDb { get; set; }

    // Units (descriptive strings)
    public string AverageSnrUnit { get; set; } = "decibels (dB)";
    public string SectionSnrUnit { get; set; } = "decibels (dB)";
    public string FramesUnit { get; set; } = "frames";
    public string ChannelsUnit { get; set; } = "count";
    public string BytesPerSampleUnit { get; set; } = "bytes";
    public string SampleRateUnit { get; set; } = "hertz (Hz)";
    public string AverageSignalUnit { get; set; } = "decibels (dB)";
    public string AverageNoiseUnit { get; set; } = "decibels (dB)";
    public string AverageOutputUnit { get; set; } = "decibels (dB)";
    public string TotalHarmonicDistortionUnit { get; set; } = "decibels (dB)";
}
