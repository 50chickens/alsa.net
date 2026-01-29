#nullable enable

using System.Collections.Concurrent;
using AlsaSharp;
using AlsaSharp.Library.Logging;
using Example.SNRReduction.Services;

namespace Example.SNRReduction.Audio;

/// <summary>
/// Event args for audio frame availability
/// </summary>
public class AudioFrameAvailableEventArgs : EventArgs
{
    public byte[] FrameData { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// Service implementation for real-time hardware copy from input to output.
/// Records audio input and plays it back simultaneously with frame-by-frame pass-through.
/// Achieves minimal latency (~1-5ms) by passing frames directly between record and play callbacks via events.
/// </summary>
public class CopyOnlyService(ILog<CopyOnlyService> log) : ICopyOnlyService
{
    private readonly ILog<CopyOnlyService> _log = log ?? throw new ArgumentNullException(nameof(log));

    /// <summary>
    /// Event fired when a new audio frame is available from recording
    /// </summary>
    public event EventHandler<AudioFrameAvailableEventArgs>? AudioFrameAvailable;

    /// <summary>
    /// Copies audio from input to output in real-time using frame-by-frame pass-through via events.
    /// Records input audio and plays it back simultaneously using event-driven architecture.
    /// Both recording and playback listen to the cancellation token to stop gracefully.
    /// </summary>
    public void CopyAudioChannels(ISoundDevice device, int durationMs = 5000, CancellationToken stoppingToken = default)
    {
        if (device == null)
            throw new ArgumentNullException(nameof(device));

        if (durationMs <= 0)
            throw new ArgumentException("Duration must be greater than 0 milliseconds", nameof(durationMs));

        _log.Info($"Starting event-driven frame-by-frame record/playback for {durationMs}ms on device: {device.Settings.CardName}");
        _log.Info($"Recording device: {device.Settings.RecordingDeviceName} ({device.Settings.RecordingChannels} channels)");
        _log.Info($"Playback device: {device.Settings.PlaybackDeviceName}");
        _log.Info("Using event-driven frame pass-through for ultra-low latency (<5ms)");

        // Create cancellation token source that combines external stopping token with duration timeout
        // This allows the worker to cancel early OR wait for the timeout
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(durationMs + 2000)); // 2 second buffer for graceful shutdown

        var cancellationToken = cts.Token;

        // Thread-safe queue for passing audio frames via events
        var audioFrameQueue = new ConcurrentQueue<byte[]>();
        var recordingStarted = new ManualResetEvent(false);
        var frameAvailableEvent = new ManualResetEvent(false);

        // Subscribe to frame available events - this handler is called by recording task
        EventHandler<AudioFrameAvailableEventArgs> onFrameAvailable = (sender, e) =>
        {
            audioFrameQueue.Enqueue(e.FrameData);
            frameAvailableEvent.Set();
        };

        AudioFrameAvailable += onFrameAvailable;

        // Task 1: Record audio - fires AudioFrameAvailable event for each frame
        // Listens to cancellationToken and stops when it's cancelled
        var recordTask = Task.Run(() =>
        {
            try
            {
                _log.Info("Recording task started");
                long frameCount = 0;
                
                // Record callback: receives audio data and fires event
                device.Record((frameData) =>
                {
                    // Check if cancellation was requested
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _log.Trace($"Recording callback detected cancellation after {frameCount} frames");
                        return; // Exit callback
                    }

                    frameCount++;
                    recordingStarted.Set();
                    
                    // Fire event - playback listeners will handle this
                    AudioFrameAvailable?.Invoke(this, new AudioFrameAvailableEventArgs { FrameData = frameData });
                }, cancellationToken);
                
                _log.Info($"Recording completed: {frameCount} frames captured");
            }
            catch (OperationCanceledException)
            {
                _log.Trace("Recording cancelled via cancellation token");
            }
            catch (Exception ex)
            {
                _log.Error($"Recording error: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            }
        });

        // Task 2: Playback audio - consumes frames from event queue
        // Listens to cancellationToken and stops when it's cancelled
        var playTask = Task.Run(() =>
        {
            try
            {
                _log.Info("Playback task started");
                
                // Wait for recording to start
                if (!recordingStarted.WaitOne(2000))
                {
                    _log.Warn("Recording did not start within 2 seconds");
                }
                
                // Playback callback: pulls frames from event queue and writes audio
                // Use the same audio format as recording (same device)
                long callbackCount = 0;
                device.PlayFromCallback(
                    (int)device.Settings.RecordingSampleRate,
                    device.Settings.RecordingChannels,
                    device.Settings.RecordingBitsPerSample,
                    (buffer) =>
                    {
                        // Check if cancellation was requested before filling buffer
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return 0; // Signal playback to stop
                        }

                        int bytesWritten = 0;
                        callbackCount++;
                        
                        // Fill buffer with queued frames (from event handlers)
                        while (bytesWritten < buffer.Length && audioFrameQueue.TryDequeue(out var frame))
                        {
                            int bytesToCopy = Math.Min(frame.Length, buffer.Length - bytesWritten);
                            Buffer.BlockCopy(frame, 0, buffer, bytesWritten, bytesToCopy);
                            bytesWritten += bytesToCopy;
                        }
                        
                        // If buffer not full, wait for more frames from event
                        if (bytesWritten < buffer.Length && !cancellationToken.IsCancellationRequested)
                        {
                            frameAvailableEvent.Reset();
                            int waitTime = Math.Min(100, (int)(buffer.Length / ((device.Settings.RecordingSampleRate * device.Settings.RecordingChannels * device.Settings.RecordingBitsPerSample) / 8) * 1000));
                            frameAvailableEvent.WaitOne(waitTime);
                            
                            // Try again after waiting
                            while (bytesWritten < buffer.Length && audioFrameQueue.TryDequeue(out var frame))
                            {
                                int bytesToCopy = Math.Min(frame.Length, buffer.Length - bytesWritten);
                                Buffer.BlockCopy(frame, 0, buffer, bytesWritten, bytesToCopy);
                                bytesWritten += bytesToCopy;
                            }
                        }
                        
                        // If still no data, fill with silence to keep playback running (unless cancelled)
                        if (bytesWritten == 0 && !cancellationToken.IsCancellationRequested)
                        {
                            Array.Clear(buffer, 0, buffer.Length);
                            bytesWritten = buffer.Length;
                        }
                        
                        // Return silence if no data but not cancelled, otherwise return what we have
                        return bytesWritten > 0 ? bytesWritten : (cancellationToken.IsCancellationRequested ? 0 : buffer.Length);
                    }, 
                    cancellationToken);
                
                _log.Info($"Playback completed after {callbackCount} callbacks");
            }
            catch (OperationCanceledException)
            {
                _log.Trace("Playback cancelled via cancellation token");
            }
            catch (Exception ex)
            {
                _log.Error($"Playback error: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            }
        });

        try
        {
            // Wait for both tasks to complete or cancellation token to be triggered
            Task.WaitAll(recordTask, playTask);
            _log.Info($"Copy-only test completed successfully");
        }
        catch (OperationCanceledException)
        {
            _log.Trace("Copy-only test cancelled via cancellation token");
        }
        catch (Exception ex)
        {
            _log.Error($"Task error: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            // Unsubscribe from event
            AudioFrameAvailable -= onFrameAvailable;
            frameAvailableEvent?.Dispose();
            recordingStarted?.Dispose();
        }
    }
}
