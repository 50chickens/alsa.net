using AlsaSharp;
using AlsaSharp.Library.Logging;
using Example.SNRReduction.Audio;

namespace Example.SNRReduction.Services;

public interface IAudioRecorderService
{
    float[] RecordToFloatArray(ISoundDevice device, uint recordDurationMs, CancellationToken token);
}
public class AudioRecorderService(ILog<AudioRecorderService> log) : IAudioRecorderService
{
    private readonly ILog<AudioRecorderService> _log = log ?? throw new ArgumentNullException(nameof(log));

    public float[] RecordToFloatArray(ISoundDevice device, uint recordDurationMs, CancellationToken token)
    {
        _log.Info($"Starting audio recording for {recordDurationMs} ms on device: {device.Settings.CardName}");
        //record audio from device to float array
        Accumulator acc = new Accumulator(device);
        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeoutCts.CancelAfter((int)recordDurationMs + 500); //add small buffer to avoid cutoff
        try
        {
            lock (this)
            {
                device.Record(acc.OnData, timeoutCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // expected when token cancels; avoid spamming logs with expected cancellations
        }
        catch (Exception ex)
        {
            _log?.Warn($"Recording task failed: {ex.Message}");
        }
        
        _log.Info($"Completed audio recording for {recordDurationMs} ms on device: {device.Settings.CardName}");
        // Convert accumulated samples to float array
        int channels = (int)(device?.Settings?.RecordingChannels ?? (uint)2);
        int totalSamples = acc.Samples * channels;
        float[] floatArray = new float[totalSamples];
        // For now, return an empty array as this would need channel-interleaved data
        // TODO: Store raw audio data in Accumulator and convert properly
        return floatArray;
    }
}