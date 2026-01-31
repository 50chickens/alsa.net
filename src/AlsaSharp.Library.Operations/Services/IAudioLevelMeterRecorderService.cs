using AlsaSharp;

namespace AlsaSharp.Library.Operations.Services;

public interface IAudioLevelMeterRecorderService
{
    List<AudioMeterLevelReading> RecordAudioMeterLevels(ISoundDevice device, CancellationToken stoppingToken);
}

public class AudioMeterLevelReading
{
    public DateTime TimestampUtc { get; set; }
    public List<double> ChannelDbfs { get; set; } = new();
    public List<double> ChannelRms { get; set; } = new();
}
