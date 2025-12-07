using AlsaSharp;
using AlsaSharp.Library.Logging;
using Example.SNRReduction.Models;
using Example.SNRReduction.Services;
using System.Text.Json;

namespace Example.SNRReduction.Services;
public class AudioLevelMeterRecorderService(ILog<AudioLevelMeterRecorderService> log, IAudioInterfaceLevelMeter audioInterfaceLevelMeter, AudioLevelMeterRecorderServiceOptions options) : IAudioLevelMeterRecorderService
{
  //constructor 

    private readonly ILog<AudioLevelMeterRecorderService> _log = log;
    private readonly AudioLevelMeterRecorderServiceOptions _options = options;
    private readonly IAudioInterfaceLevelMeter _audioInterfaceLevelMeter = audioInterfaceLevelMeter;
    
    public List<AudioMeterLevelReading> GetAudioMeterLevelReadings(TimeSpan measurementDuration, int measurementCount, string description)
    {
        if (measurementDuration <= TimeSpan.Zero) throw new ArgumentException("measureFor must be > 0", nameof(measurementDuration));
        if (measurementCount <= 0) throw new ArgumentException("measurementCount must be > 0", nameof(measurementCount));
        var results = new List<AudioMeterLevelReading>();
        List<AudioMeterLevelReading> audioLevelReadings = new List<AudioMeterLevelReading>();
        for (int i = 0; i < measurementCount; i++)
        {
            var (leftDbfs, rightDbfs) = _audioInterfaceLevelMeter.MeasureLevels((int)measurementDuration.TotalMilliseconds);
                double leftRms = double.IsNegativeInfinity(leftDbfs) ? 0.0 : Math.Pow(10.0, leftDbfs / 20.0);
                double rightRms = double.IsNegativeInfinity(rightDbfs) ? 0.0 : Math.Pow(10.0, rightDbfs / 20.0);
                audioLevelReadings.Add(new AudioMeterLevelReading
                {
                    TimestampUtc = DateTime.UtcNow,
                    LeftDbfs = leftDbfs,
                    RightDbfs = rightDbfs,
                    LeftRms = leftRms,
                    RightRms = rightRms
                });
                
                _log?.Info($"Baseline reading: L={leftDbfs:F2} dBFS R={rightDbfs:F2} dBFS");
                // brief pause to allow device to settle before next measurement
                try { System.Threading.Thread.Sleep(100); } catch { }
        }
        return audioLevelReadings;
    }
    // public void TakeAudioLevelRecording(TimeSpan measurementDuration)
    // {
    //     if (measurementDuration <= TimeSpan.Zero) throw new ArgumentException("measureFor must be > 0", nameof(measurementDuration));
    //     if (_options.MeasurementCount <= 0) throw new ArgumentException("measurementCount must be > 0", nameof(_options.MeasurementCount));
        
    //     _measurementResults = new MeasurementResult(DateTime.UtcNow, DateTime.UtcNow, measurementDuration * _options.MeasurementCount, measurementDuration, _options.Description); 

    //      _audioInterfaceLevelMeter = audioInterfaceLevelMeter;

    // }
}
