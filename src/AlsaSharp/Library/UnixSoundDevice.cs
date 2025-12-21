using AlsaSharp.Library.Logging;
namespace AlsaSharp.Library;

/// <summary>
/// Provides sound device functionality for Unix-based systems using ALSA.
/// </summary>
public class UnixSoundDevice(SoundDeviceSettings settings) : ISoundDevice
{
    private readonly ILog<UnixSoundDevice> _log = LogManager.GetLogger<UnixSoundDevice>();
    private readonly SoundDeviceSettings _settings = settings;
    private readonly AlsaPlaybackDevice _playback = new AlsaPlaybackDevice(settings);
    private readonly AlsaRecordingDevice _recording = new AlsaRecordingDevice(settings);
    private readonly AlsaMixerDevice _mixer = new AlsaMixerDevice(settings);
    private bool _disposed;
    public SoundDeviceSettings Settings => _settings;

    public long PlaybackVolume
    {
        get => _mixer.GetPlaybackVolume();
        set => _mixer.SetPlaybackVolume(value);
    }

    public bool PlaybackMute
    {
        get => _mixer.PlaybackMute;
        set => _mixer.PlaybackMute = value;
    }

    public long RecordingVolume
    {
        get => _mixer.GetRecordingVolume();
        set => _mixer.SetRecordingVolume(value);
    }

    public bool RecordingMute
    {
        get => _mixer.RecordingMute;
        set => _mixer.RecordingMute = value;
    }

    public ILog<UnixSoundDevice> Log => _log;

    #region Playback Methods

    public void Play(string wavPath)
    {
        ThrowIfDisposed();
        _playback.Play(wavPath);
    }

    public void Play(string wavPath, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        _playback.Play(wavPath, cancellationToken);
    }

    public void Play(Stream wavStream)
    {
        ThrowIfDisposed();
        _playback.Play(wavStream);
    }

    public void Play(Stream wavStream, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        _playback.Play(wavStream, cancellationToken);
    }

    #endregion

    #region Recording Methods

    public void Record(uint seconds, string savePath)
    {
        ThrowIfDisposed();
        _recording.Record(seconds, savePath);
    }

    public void Record(uint seconds, Stream saveStream)
    {
        ThrowIfDisposed();
        _recording.Record(seconds, saveStream);
    }

    public void Record(Stream saveStream, CancellationToken token)
    {
        ThrowIfDisposed();
        _recording.Record(saveStream, token);
    }

    public void Record(Action<byte[]> onDataAvailable, CancellationToken token)
    {
        ThrowIfDisposed();
        _recording.Record(onDataAvailable, token);
    }

    #endregion

    #region Advanced Mixer Control

    public int SetSimpleElementValue(string simpleElementName, string channelName, nint value)
    {
        ThrowIfDisposed();
        return _mixer.SetSimpleElementValue(simpleElementName, channelName, value);
    }

    #endregion

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(UnixSoundDevice));
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _playback.Dispose();
        _recording.Dispose();
        _mixer.Dispose();
    }
}
