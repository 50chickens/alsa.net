using AlsaSharp.Core.Native;
using AlsaSharp.Library.Logging;

namespace AlsaSharp.Library;

/// <summary>
/// Handles ALSA recording operations.
/// </summary>
internal class AlsaRecordingDevice : IDisposable
{
    private static readonly object RecordingLock = new();
    
    private readonly ILog<UnixSoundDevice>? _log;
    private readonly SoundDeviceSettings _settings;
    private readonly AlsaPcmInitializer _initializer;
    
    private IntPtr _recordingPcm;
    private bool _disposed;

    public AlsaRecordingDevice(SoundDeviceSettings settings, ILog<UnixSoundDevice>? log = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _log = log;
        _initializer = new AlsaPcmInitializer(settings, log);
    }

    public void Record(uint seconds, string savePath)
    {
        using var fs = File.Open(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
        Record(seconds, fs);
    }

    public void Record(uint seconds, Stream saveStream)
    {
        using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
        Record(saveStream, tokenSource.Token);
    }

    public void Record(Stream saveStream, CancellationToken token)
    {
        var parameters = IntPtr.Zero;
        var dir = 0;
        var header = WavHeader.Build(_settings.RecordingSampleRate, 
            _settings.RecordingChannels, _settings.RecordingBitsPerSample);
        
        header.WriteToStream(saveStream);

        OpenRecordingPcm();
        try
        {
            _initializer.InitializePcm(_recordingPcm, header, ref parameters, ref dir);
            ReadAudioStream(saveStream, header, ref parameters, ref dir, token);
        }
        finally
        {
            CloseRecordingPcm();
        }
    }

    public void Record(Action<byte[]> onDataAvailable, CancellationToken token)
    {
        var parameters = IntPtr.Zero;
        var dir = 0;
        var header = WavHeader.Build(_settings.RecordingSampleRate, 
            _settings.RecordingChannels, _settings.RecordingBitsPerSample);

        using (var memoryStream = new MemoryStream())
        {
            header.WriteToStream(memoryStream);
            onDataAvailable?.Invoke(memoryStream.ToArray());
        }

        OpenRecordingPcm();
        try
        {
            _initializer.InitializePcm(_recordingPcm, header, ref parameters, ref dir);
            ReadAudioStream(onDataAvailable, header, ref parameters, ref dir, token);
        }
        finally
        {
            CloseRecordingPcm();
        }
    }

    private unsafe void ReadAudioStream(Stream saveStream, WavHeader header, ref IntPtr @params, 
        ref int dir, CancellationToken cancellationToken)
    {
        var frames = AlsaPcmHelper.GetPeriodSize(@params, ref dir, _log);
        var bufferSize = frames * header.BlockAlign;
        var readBuffer = new byte[(int)bufferSize];

        fixed (byte* buffer = readBuffer)
        {
            while (!_disposed && !cancellationToken.IsCancellationRequested)
            {
                nint result = InteropAlsa.snd_pcm_readi(_recordingPcm, (IntPtr)buffer, frames);
                _log?.Trace($"[ALSA] snd_pcm_readi -> {result}, frames={frames}");
                
                if (result < 0)
                {
                    if (!AlsaErrorHandler.RecoverFromError(_recordingPcm, (int)result, _log))
                    {
                        AlsaErrorHandler.ValidateResult(result, ExceptionMessages.CanNotReadFromDevice, _log);
                    }
                    continue;
                }
                
                saveStream.Write(readBuffer);
            }
        }

        saveStream.Flush();
    }

    private unsafe void ReadAudioStream(Action<byte[]> onDataAvailable, WavHeader header, ref IntPtr @params, ref int dir, CancellationToken cancellationToken)
    {
        var frames = AlsaPcmHelper.GetPeriodSize(@params, ref dir, _log);
        var bufferSize = frames * header.BlockAlign;
        var readBuffer = new byte[(int)bufferSize];

        fixed (byte* buffer = readBuffer)
        {
            while (!_disposed && !cancellationToken.IsCancellationRequested)
            {
                nint result = InteropAlsa.snd_pcm_readi(_recordingPcm, (IntPtr)buffer, frames);
                
                if (result < 0)
                {
                    if (!AlsaErrorHandler.RecoverFromError(_recordingPcm, (int)result, _log))
                    {
                        AlsaErrorHandler.ValidateResult(result, ExceptionMessages.CanNotReadFromDevice, _log);
                    }
                    continue;
                }
                
                onDataAvailable?.Invoke(readBuffer);
            }
        }
    }

    private void OpenRecordingPcm()
    {
        if (_recordingPcm != IntPtr.Zero)
            return;

        lock (RecordingLock)
        {
            if (_recordingPcm != IntPtr.Zero)
                return;

            AlsaErrorHandler.ValidateResult(
                InteropAlsa.snd_pcm_open(ref _recordingPcm, _settings.RecordingDeviceName, 
                    snd_pcm_stream_t.SND_PCM_STREAM_CAPTURE, 0),
                ExceptionMessages.CanNotOpenRecording, _log);
        }
    }

    private void CloseRecordingPcm()
    {
        lock (RecordingLock)
        {
            if (_recordingPcm == IntPtr.Zero)
                return;

            AlsaPcmHelper.DropAndClosePcm(_recordingPcm, "recording", _log);
            _recordingPcm = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        CloseRecordingPcm();
    }
}
