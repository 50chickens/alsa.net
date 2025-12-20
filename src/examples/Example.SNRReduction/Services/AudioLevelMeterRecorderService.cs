#nullable enable

using AlsaSharp;
using AlsaSharp.Library.Logging;
using Example.SNRReduction.Models;

namespace Example.SNRReduction.Services;

public class AudioLevelMeterRecorderService(ILog<AudioLevelMeterRecorderService> log, AudioLevelMeterRecorderServiceOptions options, IAudioInterfaceLevelMeterService audioInterfaceLevelMeterService, FileNameGenerator fileNameGenerator) : IAudioLevelMeterRecorderService
{
    private readonly ILog<AudioLevelMeterRecorderService> _log = log ?? throw new ArgumentNullException(nameof(log));
    private readonly TimeSpan _measurementDuration = TimeSpan.FromSeconds(options.MeasurementDuration);
    private readonly int _measurementCount = options.MeasurementCount;
    private readonly IAudioInterfaceLevelMeterService _audioInterfaceLevelMeterService = audioInterfaceLevelMeterService ?? throw new ArgumentNullException(nameof(audioInterfaceLevelMeterService));
    private readonly FileNameGenerator _fileNameGenerator = fileNameGenerator ?? throw new ArgumentNullException(nameof(fileNameGenerator));
    private string? _resultFileName;
    public List<AudioMeterLevelReading> RecordAudioMeterLevels(ISoundDevice device)
    {
        if (_measurementDuration <= TimeSpan.Zero)
            throw new ArgumentException("Measurement duration must be > 0 seconds.", nameof(_measurementDuration));
        if (_measurementCount <= 0)
            throw new ArgumentException("measurementCount must be > 0", nameof(_measurementCount));
        List<AudioMeterLevelReading> _audioLevelReadings = new();

        // Generate filename on first use
        if (string.IsNullOrEmpty(_resultFileName))
        {
            _resultFileName = _fileNameGenerator.GetFileName("audio_level_recording");
        }

        for (int i = 0; i < _measurementCount; i++)
        {
            _log.Trace($"Measurement {i + 1}/{_measurementCount} ({_measurementDuration.TotalSeconds} seconds)");
            AudioMeterLevelReading audioMeterLevelReading = RecordAudioMeterLevel(device);
            _log.Info($"Result: {audioMeterLevelReading}. Completed {i + 1}/{_measurementCount} ({_measurementDuration.TotalSeconds} seconds).");
            JsonWriter jsonWriter = new JsonWriter(_resultFileName);
            jsonWriter.Append(audioMeterLevelReading);
            _audioLevelReadings.Add(audioMeterLevelReading);
            Thread.Sleep(1000);
        }
        return _audioLevelReadings;
    }
    public AudioMeterLevelReading RecordAudioMeterLevel(ISoundDevice device)
    {
        var (channelDbfs, channelRms) = _audioInterfaceLevelMeterService.MeasureLevels(device, (int)_measurementDuration.TotalMilliseconds);
        AudioMeterLevelReading audioLevelReading = new()
        {
            TimestampUtc = DateTime.UtcNow,
            ChannelDbfs = channelDbfs ?? new List<double>(),
            ChannelRms = channelRms ?? new List<double>()
        };

        return audioLevelReading;
    }
   
}
