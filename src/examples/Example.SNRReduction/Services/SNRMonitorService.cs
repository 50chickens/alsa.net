using AlsaSharp.Library.Logging;
using AlsaSharp;

namespace Example.SNRReduction.Services;

public class SNRMonitorService(ILog<SNRMonitorService> log, ISNRWorkerHelper helper, ISNRMeasurementService analyzer) : ISNRMonitorService
{
    private readonly ILog<SNRMonitorService> _log = log;
    private readonly ISNRWorkerHelper _helper = helper;
    private readonly ISNRMeasurementService _analyzer = analyzer;

    public async Task RunContinuousMonitoringAsync(ISoundDevice device, TimeSpan measureDuration, int samples, string measurementFolder, CancellationToken token)
    {
        if (device == null) return;
        var settings = device.Settings;
        if (settings == null) return;

        for (int i = 0; i < samples; i++)
        {
            using var ms = new MemoryStream();
            using var ctsRec = CancellationTokenSource.CreateLinkedTokenSource(token);
            ctsRec.CancelAfter(TimeSpan.FromSeconds(2));
            void OnData(byte[] buf) { try { ms.Write(buf, 0, buf.Length); } catch { } }
            try
            {
                device.Record(OnData, ctsRec.Token);
            }
            catch (Exception)
            {
                // ignore
            }
            ms.Flush();

            // build in-memory WAV and analyze
            var hdr = AlsaSharp.Library.WavHeader.Build((uint)settings.RecordingSampleRate, (ushort)settings.RecordingChannels, (ushort)settings.RecordingBitsPerSample);
            using var analysisMs = new MemoryStream();
            hdr.WriteToStream(analysisMs);
            ms.Seek(0, SeekOrigin.Begin);
            ms.CopyTo(analysisMs);
            analysisMs.Flush();
            analysisMs.Seek(0, SeekOrigin.Begin);

            var header = AlsaSharp.Library.WavHeader.FromStream(analysisMs);
            analysisMs.Seek(0, SeekOrigin.Begin);
            var floats = _helper.ReadWavToMonoFloat(analysisMs);

            int sr = (int)settings.RecordingSampleRate;
            int nsamples = Math.Max(1, (int)Math.Round((double)sr / 1000.0));
            int minNeeded = nsamples * 2;
            if (floats == null || floats.Length < minNeeded)
            {
                _log.Warn($"SNR sample {i + 1} too short for analysis: frames={floats?.Length ?? 0} minNeeded={minNeeded}");
                continue;
            }

            try
            {
                int hdrChannels = (int)header.NumChannels;
                int hdrBytesPerSample = Math.Max(1, (int)(header.BitsPerSample / 8));
                var res = _analyzer.AnalyzeSNR(floats, sr, 1000.0, floats.Length, hdrChannels, hdrBytesPerSample);
                if (res == null)
                {
                    _log.Warn($"SNR [{i + 1}/{samples}] analysis returned no result");
                }
                else
                {
                    _log.Trace($"SNR [{i + 1}/{samples}] Avg={res.AverageSnrDb:F2}dB Clean={res.CleanSections} Noise={res.NoiseSections}");
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"SNR sample {i + 1} failed while analyzing: {ex.Message}");
            }
        }

        await Task.CompletedTask;
    }
}
