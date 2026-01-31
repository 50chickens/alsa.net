using AlsaSharp.Core.Native;
using AlsaSharp.Library.Logging;

namespace AlsaSharp.Library;

/// <summary>
/// Handles ALSA playback operations.
/// </summary>
internal class AlsaPlaybackDevice : IDisposable
{
    private static readonly object PlaybackLock = new();
    
    private readonly ILog<UnixSoundDevice>? _log;
    private readonly SoundDeviceSettings _settings;
    private readonly AlsaPcmInitializer _initializer;
    
    private IntPtr _playbackPcm;
    private bool _disposed;

    public AlsaPlaybackDevice(SoundDeviceSettings settings, ILog<UnixSoundDevice>? log = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _log = log;
        _initializer = new AlsaPcmInitializer(settings, log);
    }

    public void Play(string wavPath)
    {
        using var fs = File.Open(wavPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        Play(fs, CancellationToken.None);
    }

    public void Play(string wavPath, CancellationToken cancellationToken)
    {
        using var fs = File.Open(wavPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        Play(fs, cancellationToken);
    }

    public void Play(Stream wavStream)
    {
        Play(wavStream, CancellationToken.None);
    }

    public void Play(Stream wavStream, CancellationToken cancellationToken)
    {
        var parameter = IntPtr.Zero;
        var dir = 0;
        var header = WavHeader.FromStream(wavStream);

        OpenPlaybackPcm();
        try
        {
            _initializer.InitializePcm(_playbackPcm, header, ref parameter, ref dir);
            WriteAudioStream(wavStream, header, ref parameter, ref dir, cancellationToken);
        }
        finally
        {
            ClosePlaybackPcm();
        }
    }

    /// <summary>
    /// Plays audio from a blocking queue-based buffer.
    /// This approach pre-buffers data before starting playback, avoiding ALSA callback timing issues.
    /// Ideal for real-time pass-through where data arrives from recording.
    /// </summary>
    /// <param name="sampleRate">Sample rate in Hz (e.g., 48000)</param>
    /// <param name="channels">Number of audio channels (e.g., 2 for stereo)</param>
    /// <param name="bitsPerSample">Bits per sample (e.g., 16)</param>
    /// <param name="dataProvider">Function that provides audio data. Returns 0 when no data available (non-blocking).</param>
    /// <param name="waitMs">How long to wait (ms) for data before returning partial frame</param>
    /// <param name="cancellationToken">Cancellation token to stop playback</param>
    public void PlayFromQueue(int sampleRate, int channels, int bitsPerSample,
        Func<byte[], int> dataProvider, int waitMs, CancellationToken cancellationToken)
    {
        if (dataProvider == null)
            throw new ArgumentNullException(nameof(dataProvider));

        var header = new WavHeader
        {
            SampleRate = (uint)sampleRate,
            NumChannels = (ushort)channels,
            BitsPerSample = (ushort)bitsPerSample,
            ByteRate = (uint)(sampleRate * channels * bitsPerSample / 8),
            BlockAlign = (ushort)(channels * bitsPerSample / 8)
        };

        var parameter = IntPtr.Zero;
        var dir = 0;

        OpenPlaybackPcm();
        try
        {
            _initializer.InitializePcm(_playbackPcm, header, ref parameter, ref dir);
            WriteAudioFromQueue(header, ref parameter, ref dir, dataProvider, waitMs, cancellationToken);
        }
        finally
        {
            ClosePlaybackPcm();
        }
    }

    private unsafe void WriteAudioFromQueue(WavHeader header, ref IntPtr @params,
        ref int dir, Func<byte[], int> dataProvider, int waitMs, CancellationToken cancellationToken)
    {
        var frames = AlsaPcmHelper.GetPeriodSize(@params, ref dir, _log);
        var bufferSize = frames * header.BlockAlign;
        var writeBuffer = new byte[(int)bufferSize];

        fixed (byte* buffer = writeBuffer)
        {
            while (!_disposed && !cancellationToken.IsCancellationRequested)
            {
                int bytesProvided = 0;
                int retries = 0;
                int maxRetries = waitMs; // 1ms per retry, so maxRetries = waitMs retries

                // Keep trying to get data until we have some or timeout
                while (bytesProvided == 0 && retries < maxRetries && !cancellationToken.IsCancellationRequested)
                {
                    bytesProvided = dataProvider(writeBuffer);
                    if (bytesProvided == 0)
                    {
                        System.Threading.Thread.Sleep(1); // Wait 1ms before retry
                        retries++;
                    }
                }

                if (bytesProvided <= 0)
                {
                    // No data available even after waiting
                    _log?.Trace("[ALSA] PlayFromQueue timeout - no data available, stream complete");
                    break;
                }

                // Calculate frames from provided bytes
                int framesToWrite = bytesProvided / header.BlockAlign;
                if (framesToWrite <= 0)
                    continue;

                nint result = InteropAlsa.snd_pcm_writei(_playbackPcm, (IntPtr)buffer, (nuint)framesToWrite);
                _log?.Trace($"[ALSA] snd_pcm_writei -> {result}, frames={framesToWrite}, bytes={bytesProvided}");
                
                if (result < 0)
                {
                    AlsaErrorHandler.ValidateResult(result, ExceptionMessages.CanNotWriteToDevice, _log);
                }
            }
        }
    }

    private unsafe void WriteAudioStream(Stream wavStream, WavHeader header, ref IntPtr @params, 
        ref int dir, CancellationToken cancellationToken)
    {
        var frames = AlsaPcmHelper.GetPeriodSize(@params, ref dir, _log);
        var bufferSize = frames * header.BlockAlign;
        var readBuffer = new byte[(int)bufferSize];

        fixed (byte* buffer = readBuffer)
        {
            while (!_disposed && !cancellationToken.IsCancellationRequested && 
                   wavStream.Read(readBuffer) != 0)
            {
                nint result = InteropAlsa.snd_pcm_writei(_playbackPcm, (IntPtr)buffer, frames);
                _log?.Trace($"[ALSA] snd_pcm_writei -> {result}, frames={frames}");
                AlsaErrorHandler.ValidateResult(result, ExceptionMessages.CanNotWriteToDevice, _log);
            }
        }
    }

    private void OpenPlaybackPcm()
    {
        if (_playbackPcm != IntPtr.Zero)
            return;

        lock (PlaybackLock)
        {
            if (_playbackPcm != IntPtr.Zero)
                return;

            AlsaErrorHandler.ValidateResult(
                InteropAlsa.snd_pcm_open(ref _playbackPcm, _settings.PlaybackDeviceName, 
                    snd_pcm_stream_t.SND_PCM_STREAM_PLAYBACK, 0),
                ExceptionMessages.CanNotOpenPlayback, _log);
        }
    }

    private void ClosePlaybackPcm()
    {
        lock (PlaybackLock)
        {
            if (_playbackPcm == IntPtr.Zero)
                return;

            AlsaPcmHelper.DropAndClosePcm(_playbackPcm, "playback", _log);
            _playbackPcm = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        ClosePlaybackPcm();
    }
}
