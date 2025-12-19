using AlsaSharp.Library.Logging;
using AlsaSharp.Library.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AlsaSharp.Library.Builders;

/// <summary>
/// Connect your sound device configuration to a virtual interface.
/// </summary>
public class UnixSoundDeviceBuilder
{
    
    /// <summary>
    /// Build sound device instances and optionally write baseline summary and per-device header JSON files
    /// into the provided measurement folder. When measurementFolder is null no files are written.
    /// </summary>
    public static IEnumerable<ISoundDevice> Build(IServiceProvider services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        var hintService = services.GetService<IHintService>();
        var log = services.GetService<ILog<UnixSoundDeviceBuilder>>();

        var list = new List<ISoundDevice>();
        var audioCardProbeService = services.GetService<AudioCardProberService>();

        foreach (var card in hintService.CardInfos)
        {
            var name = card.Id ?? card.Name ?? $"card{card.Index}";
            var soundDeviceSettings = new SoundDeviceSettings
            {
                RecordingDeviceName = $"hw:CARD={name}",
                MixerDeviceName = $"hw:CARD={name}",
                PlaybackDeviceName = $"hw:CARD={name}",
                CardId = card.Id,
                CardName = card.Name,
                CardLongName = card.LongName,
                CardIndex = card.Index
            };

            try
            {
                var supportedDeviceSettings = audioCardProbeService.Probe(soundDeviceSettings);
            }
            catch (Exception ex)
            {
                log.Warn($"Failed to probe device {soundDeviceSettings.RecordingDeviceName}: {ex.Message}");
            }

            var soundDevice = services.GetService<UnixSoundDevice>();
            
            list.Add(soundDevice);
            log.Info($"Discovered device: id={soundDeviceSettings.CardId} name={soundDeviceSettings.CardName} longname={soundDeviceSettings.CardLongName} recording={soundDeviceSettings.RecordingDeviceName} (runtime params negotiated at open)");
        }
        return list;
    }
}
