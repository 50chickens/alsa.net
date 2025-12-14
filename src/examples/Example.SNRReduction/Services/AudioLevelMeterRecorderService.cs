using AlsaSharp.Library.Extensions;
using AlsaSharp.Library.Logging;
using Example.SNRReduction.Models;

namespace Example.SNRReduction.Services;
public class AudioLevelMeterRecorderService(ILog<AudioLevelMeterRecorderService> log, IAudioInterfaceLevelMeter audioInterfaceLevelMeter, AudioLevelMeterRecorderServiceOptions options) : IAudioLevelMeterRecorderService
{
    private readonly ILog<AudioLevelMeterRecorderService> _log = log;
    private readonly AudioLevelMeterRecorderServiceOptions _options = options;
    private readonly IAudioInterfaceLevelMeter _audioInterfaceLevelMeter = audioInterfaceLevelMeter;
    
    
    public List<AudioMeterLevelReading> GetAudioMeterLevelReadings(TimeSpan measurementDuration, int measurementCount, string resultFileName)
    {
        if (measurementDuration <= TimeSpan.Zero) throw new ArgumentException("measureFor must be > 0", nameof(measurementDuration));
        if (measurementCount <= 0) throw new ArgumentException("measurementCount must be > 0", nameof(measurementCount));
        List<AudioMeterLevelReading> _audioLevelReadings = new();
        
        for (int i = 0; i < measurementCount; i++)
        {
            _log.Trace($"Measurement {i + 1}/{measurementCount} ({measurementDuration.TotalSeconds} seconds)");
            AudioMeterLevelReading audioMeterLevelReading = GetAudioMeterLevelReading(measurementDuration);
            _log.Info($"Result: {audioMeterLevelReading}. Completed {i + 1}/{measurementCount} ({measurementDuration.TotalSeconds} seconds).");
            JsonWriter jsonWriter = new JsonWriter(resultFileName);
            jsonWriter.Append(audioMeterLevelReading);
            _audioLevelReadings.Add(audioMeterLevelReading);
            Thread.Sleep(1000);
        }
        return _audioLevelReadings;
    }
    public AudioMeterLevelReading GetAudioMeterLevelReading(TimeSpan measurementDuration)
    {
        var (channelDbfs, channelRms) = _audioInterfaceLevelMeter.MeasureLevels((int)measurementDuration.TotalMilliseconds);
        AudioMeterLevelReading audioLevelReading = new()
        {
            TimestampUtc = DateTime.UtcNow,
            ChannelDbfs = channelDbfs ?? new List<double>(),
            ChannelRms = channelRms ?? new List<double>()
        };

        return audioLevelReading;
    }
}
