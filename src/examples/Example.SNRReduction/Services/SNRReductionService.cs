using AlsaSharp;
using AlsaSharp.Library.Logging;
using Example.SNRReduction.Interfaces;
using Example.SNRReduction.Models;

namespace Example.SNRReduction.Services;

public class SignalNoiseRatioOptimizer(ILog<SignalNoiseRatioOptimizer> log, ControlSweepOptions controlSweepOptions, IAudioLevelMeterRecorderService audioLevelMeterRecorderService) : IControlSweepService
{
    private readonly ILog<SignalNoiseRatioOptimizer> _log = log ?? throw new ArgumentNullException(nameof(log));
    private ControlSweepOptions _controlSweepOptions = controlSweepOptions ?? new ControlSweepOptions(new List<AlsaControl>());
    private readonly IAudioLevelMeterRecorderService _audioLevelMeterRecorderService = audioLevelMeterRecorderService ?? throw new ArgumentNullException(nameof(audioLevelMeterRecorderService));

    // small no-op logger implementation used when creating helper tools

    public List<ControlLevel> FindBestLevelsForControls(ControlSweepOptions options)
    {
        return new List<ControlLevel>()
        {
            new ControlLevel("Capture Volume", "Front Left", 32768),
            new ControlLevel("Capture Volume", "Front Right", 32768)
        };
    }

    public List<SNRSweepResult> SweepControl(ISoundDevice soundDevice, string mixerElementName, int controlMin, int controlMax, int controlStep, TimeSpan measurementDuration, int measurementCount)
    {
        var results = new List<SNRSweepResult>();
        if (soundDevice == null)
            return results;
        if (controlStep == 0)
            controlStep = 1;

        // Mixer element names to focus on (as requested). These should match your ALSA element names.
        var sweepControls = new[] {
            "Headphones",
            "Aux",
            "ADC",
            "DAC",
            "MixIn PG",
            "DAC EQ 1",
            "DAC EQ 2",
            "DAC EQ 3",
            "DAC EQ 4",
            "DAC EQ 5",
        };

        // track current values so when we sweep one control the others remain at last-chosen setting
        var currentValues = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in sweepControls)
            currentValues[c] = 0; // initial 0 dB as you described

        // helper to compute average dBFS from readings
        static double AvgDbfs(List<Example.SNRReduction.Models.AudioMeterLevelReading> r)
        {
            if (r == null || r.Count == 0)
                return double.NaN;
            double sum = 0;
            int n = 0;
            foreach (var it in r)
            {
                if (it?.ChannelDbfs == null)
                    continue;
                for (int i = 0; i < it.ChannelDbfs.Count; i++)
                {
                    var v = it.ChannelDbfs[i];
                    if (!double.IsNaN(v))
                    { sum += v; n++; }
                }
            }
            return n == 0 ? double.NaN : sum / n;
        }

        // helper to generate a short tone wav in memory
        static byte[] GenerateToneWav(int sampleRate, int channels, int bitsPerSample, double freq, int seconds, double amplitude)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            int byteRate = sampleRate * channels * bitsPerSample / 8;
            int blockAlign = channels * bitsPerSample / 8;
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + sampleRate * seconds * blockAlign);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1);
            bw.Write((short)channels);
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write((short)blockAlign);
            bw.Write((short)bitsPerSample);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            int dataSize = sampleRate * seconds * blockAlign;
            bw.Write(dataSize);
            int totalFrames = sampleRate * seconds;
            for (int i = 0; i < totalFrames; i++)
            {
                double t = i / (double)sampleRate;
                double s = Math.Sin(2.0 * Math.PI * freq * t) * amplitude;
                short sample = (short)(s * short.MaxValue);
                for (int ch = 0; ch < channels; ch++)
                    bw.Write(sample);
            }
            return ms.ToArray();
        }

        // tone params
        int sampleRate = 48000, channels = 2, bits = 16;
        double toneFreq = 1000.0;
        int measureSeconds = Math.Max(1, (int)Math.Ceiling(measurementDuration.TotalSeconds));

        // restore card state once before the entire sweep run
        try
        {
            const string statePath = "/home/pistomp/pi-stomp/setup/audio/0-db.state";
            _log.Info($"Restoring ALSA state from: {statePath} before sweep run");
            soundDevice.RestoreStateFromAlsaStateFile(statePath);
        }
        catch (Exception rex)
        {
            _log.Error(rex, "Failed to restore ALSA state before sweep run");
        }

        // measure a reference signal at the baseline state so we can confirm
        // that signal strength does not drop while we try to reduce noise.
        double referenceSignalDb = double.NaN;
        try
        {
            var refTone = GenerateToneWav(sampleRate, channels, bits, toneFreq, measureSeconds, 0.5);
            List<Example.SNRReduction.Models.AudioMeterLevelReading> refReadings = new List<Example.SNRReduction.Models.AudioMeterLevelReading>();
            var refTask = System.Threading.Tasks.Task.Run(() => _audioLevelMeterRecorderService.GetAudioMeterLevelReadings(measurementDuration, measurementCount, "ReferenceSignal"));
            try
            {
                using var msRef = new MemoryStream(refTone);
                soundDevice.Play(msRef, System.Threading.CancellationToken.None);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Reference signal play failed");
            }
            try
            { refReadings = refTask.Result; }
            catch { refReadings = new List<Example.SNRReduction.Models.AudioMeterLevelReading>(); }
            referenceSignalDb = AvgDbfs(refReadings);
            _log.Info($"Reference signal level: {referenceSignalDb:F2} dBFS");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Reference measurement failed");
        }

        // iterate controls; for each, sweep its range while keeping others at currentValues
        foreach (var control in sweepControls)
        {
            var controlKey = control ?? "(unknown)";
            for (int val = controlMin; val <= controlMax; val += controlStep)
            {
                try
                {
                    // set other controls to their current values first
                    foreach (var kv in currentValues)
                    {
                        var k = kv.Key ?? "(unknown)";
                        try
                        {
                            soundDevice.SetSimpleElementValue(k, "Front Left", (nint)kv.Value);
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, $"Failed to set {k} Front Left to {kv.Value}");
                        }
                        try
                        {
                            soundDevice.SetSimpleElementValue(k, "Front Right", (nint)kv.Value);
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, $"Failed to set {k} Front Right to {kv.Value}");
                        }
                    }

                    // set the control under test to new value
                    try
                    {
                        soundDevice.SetSimpleElementValue(control ?? "(unknown)", "Front Left", (nint)val);
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, $"Failed to set {control ?? "(unknown)"} Front Left to {val}");
                    }
                    try
                    {
                        soundDevice.SetSimpleElementValue(control ?? "(unknown)", "Front Right", (nint)val);
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, $"Failed to set {control ?? "(unknown)"} Front Right to {val}");
                    }

                    // settle
                    Thread.Sleep(150);

                    // measure noise (no tone)
                    List<AudioMeterLevelReading> noiseReadings = new List<AudioMeterLevelReading>();
                    var noiseTask = System.Threading.Tasks.Task.Run(() => _audioLevelMeterRecorderService.GetAudioMeterLevelReadings(measurementDuration, measurementCount, $"Noise {control ?? "(unknown)"}={val}"));
                    try
                    {
                        noiseReadings = noiseTask.Result;
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"Noise measurement task failed for {control ?? "(unknown)"}={val}: {ex.Message}");
                        noiseReadings = new List<Example.SNRReduction.Models.AudioMeterLevelReading>();
                    }

                    double noiseDb = AvgDbfs(noiseReadings);

                    // generate tone and play while recording
                    var tone = GenerateToneWav(sampleRate, channels, bits, toneFreq, measureSeconds, 0.5);
                    List<Example.SNRReduction.Models.AudioMeterLevelReading> sigReadings = new List<Example.SNRReduction.Models.AudioMeterLevelReading>();
                    var recordTask = System.Threading.Tasks.Task.Run(() => _audioLevelMeterRecorderService.GetAudioMeterLevelReadings(measurementDuration, measurementCount, $"Signal {control ?? "(unknown)"}={val}"));
                    try
                    {
                        // play tone (blocking until done)
                        using var msTone = new MemoryStream(tone);
                        soundDevice.Play(msTone, System.Threading.CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"Tone play failed for {control ?? "(unknown)"}={val}: {ex.Message}");
                    }
                    try
                    {
                        sigReadings = recordTask.Result;
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"Signal measurement task failed for {control ?? "(unknown)"}={val}: {ex.Message}");
                        sigReadings = new List<Example.SNRReduction.Models.AudioMeterLevelReading>();
                    }

                    double signalDb = AvgDbfs(sigReadings);

                    double snrDb = double.IsNaN(signalDb) || double.IsNaN(noiseDb) ? double.NaN : (signalDb - noiseDb);

                    // compare signal to reference and warn if the signal dropped
                    try
                    {
                        if (!double.IsNaN(referenceSignalDb) && !double.IsNaN(signalDb))
                        {
                            double delta = signalDb - referenceSignalDb;
                            if (delta < -1.0)
                            {
                                _log.Warn($"Signal level dropped by {Math.Abs(delta):F2} dB from reference for {control ?? "(unknown)"}={val}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Failed while comparing signal to reference");
                    }

                    results.Add(new SNRSweepResult(controlKey, "Both", val, signalDb, noiseDb, snrDb));
                }
                catch (Exception ex)
                {
                    _log?.Error(ex, $"Sweep error for control {control} value {val}");
                }
            }

            // pick best value for this control (highest SNR) and keep it
            double bestSNR = double.NegativeInfinity;
            int bestVal = currentValues[controlKey];
            foreach (var r in results)
            {
                if (!string.Equals(r.ControlName, controlKey, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (double.IsNaN(r.SNRdB))
                    continue;
                if (r.SNRdB > bestSNR)
                { bestSNR = r.SNRdB; bestVal = (int)r.Value; }
            }
            currentValues[controlKey] = bestVal;
            // ensure device uses bestVal
            try
            { soundDevice.SetSimpleElementValue(controlKey, "Front Left", (nint)bestVal); }
            catch (Exception ex) { _log.Warn($"Failed to set {controlKey} Front Left to {bestVal}: {ex.Message}"); }
            try
            { soundDevice.SetSimpleElementValue(controlKey, "Front Right", (nint)bestVal); }
            catch (Exception ex) { _log.Warn($"Failed to set {controlKey} Front Right to {bestVal}: {ex.Message}"); }
        }


        // write results to a timestamped json file in the current working directory
        try
        {
            var outFile = $"snr_sweep_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
            var writer = new AlsaSharp.Library.Logging.JsonWriter(outFile);
            var payload = new { TimestampUtc = DateTime.UtcNow, ReferenceSignalDb = referenceSignalDb, Sweep = results };
            writer.Append(payload);
            _log.Info($"Wrote sweep results to {outFile}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to write sweep results to JSON file");
        }

        return results;
    }

}
