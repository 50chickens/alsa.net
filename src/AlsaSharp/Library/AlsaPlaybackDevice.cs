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
                _log?.Trace(m => m("[ALSA] snd_pcm_writei -> {0}, frames={1}", result, frames));
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
