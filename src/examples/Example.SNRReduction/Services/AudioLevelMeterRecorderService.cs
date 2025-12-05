using AlsaSharp;
using AlsaSharp.Library.Logging;
using Example.SNRReduction.Models;
using Example.SNRReduction.Services;
using System.Text.Json;

namespace Examples.SNRReduction.Services;
public class AudioLevelMeterRecorderService(ILog<AudioLevelMeterRecorderService> log, IAudioInterfaceLevelMeter audioInterfaceLevelMeter, AudioLevelMeterRecorderServiceOptions options) : IAudioLevelMeterRecorderService
{
  //constructor 

    private readonly ILog<AudioLevelMeterRecorderService> _log = log;
    private readonly AudioLevelMeterRecorderServiceOptions _options = options;
    private IAudioInterfaceLevelMeter _audioInterfaceLevelMeter;
    
    public List<AudioMeterLevelReading> GetAudioMeterLevelReadings(TimeSpan measurementDuration, int measurementCount, string description)
    {
        if (measurementDuration <= TimeSpan.Zero) throw new ArgumentException("measureFor must be > 0", nameof(measurementDuration));
        if (measurementCount <= 0) throw new ArgumentException("measurementCount must be > 0", nameof(measurementCount));
        var results = new List<AudioMeterLevelReading>();
        _audioInterfaceLevelMeter = audioInterfaceLevelMeter;
        List<AudioMeterLevelReading> audioLevelReadings = new List<AudioMeterLevelReading>();
        new int[_options.MeasurementCount].ToList().ForEach(i =>
         {
           audioLevelReadings.Add(new AudioMeterLevelReading());
              var (leftDbfs, rightDbfs) = _audioInterfaceLevelMeter.MeasureLevels((int)measurementDuration.TotalSeconds);
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

         });
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
