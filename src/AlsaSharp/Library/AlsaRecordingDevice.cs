using System;
using System.IO;
using System.Threading;
using AlsaSharp.Core.Native;
using Common.Logging;

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
