using AlsaSharp;
using AlsaSharp.Library.Logging;

namespace AlsaSharp.Library.Operations.Services;

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
        
        // TODO: Implement proper audio recording to float array conversion
        // This is a placeholder that needs to be completed with:
        // 1. Record audio data using the Accumulator
        // 2. Convert the recorded byte data to float samples
        // 3. Handle channel interleaving properly
        // 4. Return the actual recorded float array instead of empty array
        
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
        // PLACEHOLDER: This needs to be implemented to convert actual audio data
        // The Accumulator currently only tracks sample statistics, not raw audio data
        return floatArray;
    }
}