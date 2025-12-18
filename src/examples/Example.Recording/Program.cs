using AlsaSharp;
using AlsaSharp.Library.Builders;

namespace Example.Recording;

class Program
{
    static void Main()
    {
        // create virtual interface to system default audio device
        using var alsaDevice = AlsaDeviceBuilder.Build(new SoundDeviceSettings());

        // record 5 seconds directly to a diagnostic WAV (allowed per user's instruction)
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var folder = Path.Combine(home, ".SNRReduction");
        Directory.CreateDirectory(folder);
        var outPath = Path.Combine(folder, "alsabat_diag.wav");
        Console.WriteLine($"Recording diagnostic WAV to: {outPath}");
        alsaDevice.Record(5u, outPath);
    }
}