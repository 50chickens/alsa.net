using AlsaSharp;
using AlsaSharp.Library.Logging;

namespace AlsaSharp.Library.Operations.Services;

public class AudioLevelMeterRecorderServiceOptions
{
    public double MeasurementDuration { get; set; } = 5.0;
    public int MeasurementCount { get; set; } = 1;
}

public class AudioLevelMeterRecorderService(ILog<AudioLevelMeterRecorderService> log, AudioLevelMeterRecorderServiceOptions options) : IAudioLevelMeterRecorderService
{
    private readonly ILog<AudioLevelMeterRecorderService> _log = log ?? throw new ArgumentNullException(nameof(log));
    private readonly TimeSpan _measurementDuration = TimeSpan.FromSeconds(options.MeasurementDuration);
    private readonly int _measurementCount = options.MeasurementCount;

    public List<AudioMeterLevelReading> RecordAudioMeterLevels(ISoundDevice device, CancellationToken cancellationToken)
    {
        if (_measurementDuration <= TimeSpan.Zero)
            throw new ArgumentException("Measurement duration must be > 0 seconds.", nameof(_measurementDuration));
        if (_measurementCount <= 0)
            throw new ArgumentException("measurementCount must be > 0", nameof(_measurementCount));
        
        List<AudioMeterLevelReading> audioLevelReadings = new();

        for (int i = 0; i < _measurementCount; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _log.Info("Audio level measurement cancelled.");
                break;
            }
            
            _log.Trace($"Measurement {i + 1}/{_measurementCount} ({_measurementDuration.TotalSeconds} seconds)");
            AudioMeterLevelReading audioMeterLevelReading = RecordAudioMeterLevel(device, cancellationToken);
            _log.Info($"Completed {i + 1}/{_measurementCount} measurements.");
            audioLevelReadings.Add(audioMeterLevelReading);
            
            if (i < _measurementCount - 1)
                Thread.Sleep(1000);
        }
        
        return audioLevelReadings;
    }

    private AudioMeterLevelReading RecordAudioMeterLevel(ISoundDevice device, CancellationToken cancellationToken)
    {
        var (channelDbfs, channelRms) = MeasureLevels(device, (int)_measurementDuration.TotalMilliseconds, cancellationToken);
        
        return new AudioMeterLevelReading
        {
            TimestampUtc = DateTime.UtcNow,
            ChannelDbfs = channelDbfs ?? new List<double>(),
            ChannelRms = channelRms ?? new List<double>()
        };
    }

    private (List<double>? channelDbfs, List<double>? channelRms) MeasureLevels(ISoundDevice device, int durationMs, CancellationToken cancellationToken)
    {
        // TODO: Implement actual audio level measurement using device recording
        // This is a placeholder implementation that should be replaced with proper level detection
        // The actual implementation should:
        // 1. Record audio from the device for the specified duration
        // 2. Analyze the recorded samples to calculate RMS and dBFS values per channel
        // 3. Return the actual measured values instead of placeholders
        
        var channelCount = device.Settings.RecordingChannels;
        var channelDbfs = new List<double>();
        var channelRms = new List<double>();

        for (int i = 0; i < channelCount; i++)
        {
            channelDbfs.Add(-20.0); // Placeholder
            channelRms.Add(0.1); // Placeholder
        }

        return (channelDbfs, channelRms);
    }
}

