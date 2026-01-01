using System.IO;
using AlsaSharp;
using Example.SNRReduction.Audio;

namespace Example.SNRReduction.Services;

public interface ISNRWorkerHelper
{
    MemoryStream BuildInlineSineWav(SoundDeviceSettings settings, double frequencyHz, double durationSeconds);
    float[] ReadWavToMonoFloat(Stream wavStream);
    string SanitizeFileName(string s);
    Accumulator CreateAccumulator(ISoundDevice device);

    
}
