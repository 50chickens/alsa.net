#nullable enable

using System.Runtime.InteropServices;
using AlsaSharp.Core.Native;
using AlsaSharp.Library.Logging;

namespace AlsaSharp.Library.Services;

/// <summary>
/// Service that generates and plays test tones through ALSA PCM devices.
/// </summary>
public class TestToneService(ILog<TestToneService> log) : ITestToneService
{
    private readonly ILog<TestToneService> _log = log ?? throw new ArgumentNullException(nameof(log));
    
    private const int SampleRate = 48000;
    private const snd_pcm_format_t Format = snd_pcm_format_t.SND_PCM_FORMAT_S16_LE;
    private const snd_pcm_access_t Access = snd_pcm_access_t.SND_PCM_ACCESS_RW_INTERLEAVED;
    private const uint Channels = 2;
    
    /// <summary>
    /// Plays a test tone with the specified parameters through the given device.
    /// </summary>
    /// <param name="deviceName">The ALSA device name (e.g., "hw:CARD=sndrpihifiberry,DEV=0")</param>
    /// <param name="frequencyHz">The frequency of the tone in Hz</param>
    /// <param name="amplitudeDbfs">The amplitude in dBFS</param>
    /// <param name="leftChannelDurationMs">Duration for left channel only tone in milliseconds</param>
    /// <param name="rightChannelDurationMs">Duration for right channel only tone in milliseconds</param>
    /// <param name="bothChannelsDurationMs">Duration for both channels tone in milliseconds</param>
    public void PlayTestTone(string deviceName, int frequencyHz, double amplitudeDbfs, 
        int leftChannelDurationMs, int rightChannelDurationMs, int bothChannelsDurationMs)
    {
        IntPtr pcmHandle = IntPtr.Zero;
        
        try
        {
            // Open PCM device
            int err = InteropAlsa.snd_pcm_open(ref pcmHandle, deviceName, snd_pcm_stream_t.SND_PCM_STREAM_PLAYBACK, 0);
            if (err < 0)
            {
                throw new InvalidOperationException($"Failed to open PCM device '{deviceName}': {GetErrorString(err)}");
            }
            
            _log.Trace($"Opened PCM device: {deviceName}");
            
            // Set hardware parameters with larger buffer and timeout to prevent underruns
            // Use 1 second timeout (1000000µs) to allow sufficient time for sample preparation
            err = InteropAlsa.snd_pcm_set_params(pcmHandle, Format, Access, Channels, SampleRate, 1, 1000000);
            if (err < 0)
            {
                throw new InvalidOperationException($"Failed to set PCM parameters: {GetErrorString(err)}");
            }
            
            _log.Trace($"Set PCM parameters: {SampleRate}Hz, {Channels} channels, format: {Format}");
            
            // Calculate amplitude from dBFS
            double amplitude = Math.Pow(10.0, amplitudeDbfs / 20.0);
            short maxAmplitude = (short)(short.MaxValue * amplitude);
            
            _log.Info($"Playing 440Hz test tone: Left={leftChannelDurationMs}ms, Right={rightChannelDurationMs}ms, Both={bothChannelsDurationMs}ms");
            
            // Play left channel only (tone in left, nothing in right)
            _log.Info($"[LEFT CHANNEL] Playing test tone for {leftChannelDurationMs}ms");
            PlayMonoToStereoTone(pcmHandle, frequencyHz, maxAmplitude, leftChannelDurationMs, PlayChannel.Left);
            
            // Pause between tones
            System.Threading.Thread.Sleep(1000);
            
            // Play right channel only (nothing in left, tone in right)
            _log.Info($"[RIGHT CHANNEL] Playing test tone for {rightChannelDurationMs}ms");
            PlayMonoToStereoTone(pcmHandle, frequencyHz, maxAmplitude, rightChannelDurationMs, PlayChannel.Right);
            
            // Pause between tones
            System.Threading.Thread.Sleep(1000);
            
            // Play both channels
            _log.Info($"[BOTH CHANNELS] Playing test tone for {bothChannelsDurationMs}ms");
            PlayMonoToStereoTone(pcmHandle, frequencyHz, maxAmplitude, bothChannelsDurationMs, PlayChannel.Both);
            
            // Drain the PCM to ensure all data is played
            InteropAlsa.snd_pcm_drain(pcmHandle);
            _log.Info("Test tone playback completed");
        }
        finally
        {
            if (pcmHandle != IntPtr.Zero)
            {
                InteropAlsa.snd_pcm_close(pcmHandle);
                _log.Trace("Closed PCM device");
            }
        }
    }
    
    private enum PlayChannel
    {
        Left,
        Right,
        Both
    }
    
    private void PlayMonoToStereoTone(IntPtr pcmHandle, int frequencyHz, short amplitude, int durationMs, PlayChannel channel)
    {
        int numSamples = (SampleRate * durationMs) / 1000;
        int periodSize = 4096;  // Increased from 2048 to reduce buffer pressure
        int bytesPerSample = sizeof(short);
        
        // Pre-allocate buffer for stereo (2 channels) samples outside the loop
        byte[] buffer = new byte[periodSize * (int)Channels * bytesPerSample];
        GCHandle bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        
        int samplesWritten = 0;
        
        _log.Trace($"Playing {channel} channel(s): {frequencyHz}Hz, amplitude: {amplitude}, duration: {durationMs}ms");
        
        try
        {
            // Pre-buffer some silence to fill the device buffer and prevent initial underruns
            int preBufferSize = periodSize * 4;  // Pre-buffer 4 periods of silence
            int preBufferSamples = 0;
            while (preBufferSamples < preBufferSize && samplesWritten < numSamples)
            {
                int samplesToWrite = Math.Min(periodSize, preBufferSize - preBufferSamples);
                
                // Fill with silence (zeros) for initial buffer
                Array.Clear(buffer, 0, samplesToWrite * (int)Channels * bytesPerSample);
                
                nint written = InteropAlsa.snd_pcm_writei(pcmHandle, bufferHandle.AddrOfPinnedObject(), (nuint)samplesToWrite);
                if (written < 0)
                {
                    int recoverErr = InteropAlsa.snd_pcm_recover(pcmHandle, (int)written, 0);
                    if (recoverErr < 0)
                    {
                        _log.Warn($"Pre-buffer recovery failed: {GetErrorString(recoverErr)}");
                    }
                }
                else
                {
                    preBufferSamples += (int)written;
                }
            }
            
            // Now play the actual tone
            while (samplesWritten < numSamples)
            {
                int samplesToWrite = Math.Min(periodSize, numSamples - samplesWritten);
                
                // Generate samples
                for (int i = 0; i < samplesToWrite; i++)
                {
                    double t = (double)(samplesWritten + i) / SampleRate;
                    double sineValue = Math.Sin(2.0 * Math.PI * frequencyHz * t);
                    short sample = (short)(amplitude * sineValue);
                    
                    int bufferIndex = i * (int)Channels * bytesPerSample;
                    
                    switch (channel)
                    {
                        case PlayChannel.Left:
                            // Only left channel has tone
                            BitConverter.GetBytes(sample).CopyTo(buffer, bufferIndex);
                            BitConverter.GetBytes((short)0).CopyTo(buffer, bufferIndex + bytesPerSample);
                            break;
                        case PlayChannel.Right:
                            // Only right channel has tone
                            BitConverter.GetBytes((short)0).CopyTo(buffer, bufferIndex);
                            BitConverter.GetBytes(sample).CopyTo(buffer, bufferIndex + bytesPerSample);
                            break;
                        case PlayChannel.Both:
                            // Both channels have the same tone
                            BitConverter.GetBytes(sample).CopyTo(buffer, bufferIndex);
                            BitConverter.GetBytes(sample).CopyTo(buffer, bufferIndex + bytesPerSample);
                            break;
                    }
                }
                
                // Write samples to PCM device with retry logic for underruns
                nint written = InteropAlsa.snd_pcm_writei(pcmHandle, bufferHandle.AddrOfPinnedObject(), (nuint)samplesToWrite);
                if (written < 0)
                {
                    // Attempt to recover from underrun
                    int recoverErr = InteropAlsa.snd_pcm_recover(pcmHandle, (int)written, 0);
                    if (recoverErr < 0)
                    {
                        _log.Warn($"PCM write error: {GetErrorString((int)written)}, recovery failed: {GetErrorString(recoverErr)}");
                        // Continue anyway - recovery messages are expected in some ALSA configurations
                        continue;
                    }
                    // After recovery, retry the write
                    written = InteropAlsa.snd_pcm_writei(pcmHandle, bufferHandle.AddrOfPinnedObject(), (nuint)samplesToWrite);
                    if (written > 0)
                    {
                        samplesWritten += (int)written;
                    }
                }
                else
                {
                    samplesWritten += (int)written;
                }
            }
        }
        finally
        {
            bufferHandle.Free();
        }
    }
    
    private string GetErrorString(int errCode)
    {
        IntPtr errorPtr = InteropAlsa.snd_strerror(errCode);
        return Marshal.PtrToStringAnsi(errorPtr) ?? $"Unknown error ({errCode})";
    }
}
