using AlsaSharp.Library.Logging;
using AlsaSharp.Library.Services;

namespace AlsaSharp.Library.Builders;

/// <summary>
/// Connect your sound device configuration to a virtual interface.
/// </summary>
public class AudioDeviceBuilder(ILog<AudioDeviceBuilder> log, AudioCardProberService audioCardProberService, HintService hintService) : IAudioDeviceBuilder
{
    private readonly ILog<AudioDeviceBuilder> _log = log;
    private readonly AudioCardProberService _audioCardProberService = audioCardProberService;
    private HintService _hintService = hintService;
    public IEnumerable<ISoundDevice> BuildAudioDevices()
    {
        
        var list = new List<ISoundDevice>();
        
        foreach (var card in _hintService.CardInfos)
        {
            _log.Info($"Found audio card: index={card.Index} id={card.Id} name={card.Name} longname={card.LongName}");
            var name = card.Id ?? card.Name ?? $"card{card.Index}";
            var soundDeviceSettings = new SoundDeviceSettings
            {
                RecordingDeviceName = $"hw:CARD={name}",
                MixerDeviceName = $"hw:CARD={name}",
                PlaybackDeviceName = $"hw:CARD={name}",
                CardId = card.Id,
                CardName = card.Name,
                CardLongName = card.LongName,
                CardIndex = card.Index,
                
            };

            try
            {
                _log.Info($"Probing device {soundDeviceSettings.RecordingDeviceName}...");
                soundDeviceSettings = _audioCardProberService.Probe(soundDeviceSettings);
            }
            catch (Exception ex)
            {
                log.Warn($"Failed to probe device {soundDeviceSettings.RecordingDeviceName}: {ex.Message}");
            }

            list.Add(new UnixSoundDevice(soundDeviceSettings));
            log.Info($"Discovered device: id={soundDeviceSettings.CardId} name={soundDeviceSettings.CardName} longname={soundDeviceSettings.CardLongName} recording={soundDeviceSettings.RecordingDeviceName} (runtime params negotiated at open)");
        }
        return list;
    }

    
}
