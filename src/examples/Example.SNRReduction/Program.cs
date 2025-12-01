using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Alsa.Net;

using Alsa.Net.Internal;
using System.Text.RegularExpressions;

namespace Example.SNRReduction
{
    partial class Program
    {
        static async Task<int> Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddCommandLine(args)
                .Build();

            var config = configuration.Get<AppConfig>();
            var baselineOnly = args.Any(a => a.Equals("--baseline", StringComparison.OrdinalIgnoreCase)) || configuration.GetValue<bool>("BaselineOnly");

            Console.WriteLine("Example.SNRReduction: running baseline measurement...");
            
            if (!new AlsaCardEnumerator().TryGetCards(out var cards))
            {
                Console.WriteLine("No ALSA sound cards found");
                return 1;
            }

            var chosenCard = cards.Where(c => Regex.IsMatch(c.Name, config.CardName, RegexOptions.IgnoreCase)).FirstOrDefault();

            if (chosenCard == null)
            {
                Console.Error.WriteLine($"Card matching {config.CardName} not found");
                return 1;
            }

            Console.WriteLine($"Using card {chosenCard.Name} (id={chosenCard.Index})");

            var settings = new SoundDeviceSettings
            {
                PlaybackDeviceName = $"hw:CARD={chosenCard.Name}",
                RecordingDeviceName = $"hw:CARD={chosenCard.Name}",
                MixerDeviceName = $"hw:CARD={chosenCard.Name}",
                RecordingSampleRate = 48000,
                RecordingChannels = 2,
                RecordingBitsPerSample = 16
            };

            using var device = AlsaDeviceBuilder.Create(settings);

            Console.WriteLine("Measuring baseline noise (silence)...");
            var baselineNoise = new ProgramHelpers().MeasureNoise(device, config.BaselineSeconds);
            Console.WriteLine($"Baseline noise RMS: {baselineNoise:E3}");

            Console.WriteLine("Measuring baseline signal (tone)...");
            var baselineSignal = await new ProgramHelpers().MeasureSignalAsync(device, config.SignalSeconds, config.TestToneHz);
            Console.WriteLine($"Baseline signal RMS: {baselineSignal:E3}");

            var result = new { Card = chosenCard.Index, CardName = chosenCard.Name, BaselineNoise = baselineNoise, BaselineSignal = baselineSignal };
            File.WriteAllText(config.ResultsFile, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Wrote baseline results to {config.ResultsFile}");

            return 0;
        }
    }
}
