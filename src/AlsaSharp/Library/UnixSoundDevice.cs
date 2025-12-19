using System.Runtime.InteropServices;
using AlsaSharp.Core.Native;
using Microsoft.Extensions.Logging;

namespace AlsaSharp.Library;

class UnixSoundDevice(SoundDeviceSettings settings, ILogger<UnixSoundDevice>? log = null) : ISoundDevice
{
    private readonly ILogger<UnixSoundDevice>? _log = log;
    static readonly object PlaybackInitializationLock = new();
    static readonly object RecordingInitializationLock = new();
    static readonly object MixerInitializationLock = new();

    public SoundDeviceSettings Settings { get; } = settings;
    public long PlaybackVolume { get => NativeWidth.FromNint(GetPlaybackVolume()); set => SetPlaybackVolume(NativeWidth.ToNint(value)); }
    public bool PlaybackMute { get => _playbackMute; set => SetPlaybackMute(value); }
    public long RecordingVolume { get => NativeWidth.FromNint(GetRecordingVolume()); set => SetRecordingVolume(NativeWidth.ToNint(value)); }
    public bool RecordingMute { get => _recordingMute; set => SetRecordingMute(value); }

    bool _playbackMute;
    bool _recordingMute;
    IntPtr _playbackPcm;
    IntPtr _recordingPcm;
    IntPtr _mixer;
    IntPtr _mixerElement;
    bool _wasDisposed;

    public void Play(string wavPath)
    {
        if (_wasDisposed)
            throw new ObjectDisposedException(nameof(UnixSoundDevice));

        using var fs = File.Open(wavPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        Play(fs, CancellationToken.None);
    }

    public void Play(string wavPath, CancellationToken cancellationToken)
    {
        if (_wasDisposed)
            throw new ObjectDisposedException(nameof(UnixSoundDevice));

        using var fs = File.Open(wavPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        Play(fs, cancellationToken);
    }

    public void Play(Stream wavStream)
    {
        if (_wasDisposed)
            throw new ObjectDisposedException(nameof(UnixSoundDevice));

        Play(wavStream, CancellationToken.None);
    }

    public void Play(Stream wavStream, CancellationToken cancellationToken)
    {
        if (_wasDisposed)
            throw new ObjectDisposedException(nameof(UnixSoundDevice));

        var parameter = new IntPtr();
        var dir = 0;
        var header = WavHeader.FromStream(wavStream);

        OpenPlaybackPcm();
        PcmInitialize(_playbackPcm, header, ref parameter, ref dir);
        WriteStream(wavStream, header, ref parameter, ref dir, cancellationToken);
        ClosePlaybackPcm();
    }

    public void Record(uint second, string savePath)
    {
        if (_wasDisposed)
            throw new ObjectDisposedException(nameof(UnixSoundDevice));

        using var fs = File.Open(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
        Record(second, fs);
    }

    public void Record(uint second, Stream saveStream)
    {
        if (_wasDisposed)
            throw new ObjectDisposedException(nameof(UnixSoundDevice));

        using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(second));
        Record(saveStream, tokenSource.Token);
    }

    public void Record(Stream saveStream, CancellationToken token)
    {
        if (_wasDisposed)
            throw new ObjectDisposedException(nameof(UnixSoundDevice));

        var parameters = new IntPtr();
        var dir = 0;
        var header = WavHeader.Build(Settings.RecordingSampleRate, Settings.RecordingChannels, Settings.RecordingBitsPerSample);
        header.WriteToStream(saveStream);

        OpenRecordingPcm();
        PcmInitialize(_recordingPcm, header, ref parameters, ref dir);
        ReadStream(saveStream, header, ref parameters, ref dir, token);
        CloseRecordingPcm();
    }

    public void Record(Action<byte[]> onDataAvailable, CancellationToken token)
    {
        if (_wasDisposed)
            throw new ObjectDisposedException(nameof(UnixSoundDevice));

        var parameters = new IntPtr();
        var dir = 0;

        var header = WavHeader.Build(Settings.RecordingSampleRate, Settings.RecordingChannels, Settings.RecordingBitsPerSample);
        using (var memoryStream = new MemoryStream())
        {
            header.WriteToStream(memoryStream);
            onDataAvailable.Invoke(memoryStream.ToArray());
        }

        OpenRecordingPcm();
        PcmInitialize(_recordingPcm, header, ref parameters, ref dir);
        ReadStream(onDataAvailable, header, ref parameters, ref dir, token);
        CloseRecordingPcm();
    }

    unsafe void WriteStream(Stream wavStream, WavHeader header, ref IntPtr @params, ref int dir, CancellationToken cancellationToken)
    {
        nuint frames;

        fixed (int* dirP = &dir)
        {
            int rv = InteropAlsa.snd_pcm_hw_params_get_period_size(@params, &frames, dirP);
            _log?.LogTrace("[ALSA DEBUG] snd_pcm_hw_params_get_period_size -> {Rv}, frames={Frames}", rv, frames);
            ThrowErrorMessage(rv, ExceptionMessages.CanNotGetPeriodSize);
        }

        var bufferSize = frames * header.BlockAlign;
        var readBuffer = new byte[(int)bufferSize];

        fixed (byte* buffer = readBuffer)
        {
            while (!_wasDisposed && !cancellationToken.IsCancellationRequested && wavStream.Read(readBuffer) != 0)
            {
                nint rv = InteropAlsa.snd_pcm_writei(_playbackPcm, (IntPtr)buffer, frames);
                _log?.LogTrace("[ALSA DEBUG] snd_pcm_writei -> {Rv}, frames requested={Frames}", rv, frames);
                ThrowErrorMessage(rv, ExceptionMessages.CanNotWriteToDevice);
            }
        }
    }

    unsafe void ReadStream(Stream saveStream, WavHeader header, ref IntPtr @params, ref int dir, CancellationToken cancellationToken)
    {
        nuint frames;
        fixed (int* dirP = &dir)
        {
            int rv = InteropAlsa.snd_pcm_hw_params_get_period_size(@params, &frames, dirP);
            _log?.LogTrace("[ALSA DEBUG] snd_pcm_hw_params_get_period_size -> {Rv}, frames={Frames}", rv, frames);
            ThrowErrorMessage(rv, ExceptionMessages.CanNotGetPeriodSize);
        }

        var bufferSize = frames * header.BlockAlign;
        var readBuffer = new byte[(int)bufferSize];

        fixed (byte* buffer = readBuffer)
        {
            while (!_wasDisposed && !cancellationToken.IsCancellationRequested)
            {
                nint rv = InteropAlsa.snd_pcm_readi(_recordingPcm, (IntPtr)buffer, frames);
                _log?.LogTrace("[ALSA DEBUG] snd_pcm_readi -> {Rv}, frames requested={Frames}", rv, frames);
                if (rv < 0)
                {
                    // try to recover from transient I/O errors (eg. -EIO)
                    int rec = InteropAlsa.snd_pcm_recover(_recordingPcm, (int)rv, 0);
                    if (rec < 0)
                    {
                        _log?.LogError("[ALSA ERROR] snd_pcm_recover failed: {Error}", InteropAlsa.StrError(rec));
                        ThrowErrorMessage(rec, ExceptionMessages.CanNotReadFromDevice);
                    }
                    // recovered — try next read iteration
                    continue;
                }
                saveStream.Write(readBuffer);
            }
        }

        saveStream.Flush();
    }

    unsafe void ReadStream(Action<byte[]> onDataAvailable, WavHeader header, ref IntPtr @params, ref int dir, CancellationToken cancellationToken)
    {
        nuint frames;

        fixed (int* dirP = &dir)
            ThrowErrorMessage(InteropAlsa.snd_pcm_hw_params_get_period_size(@params, &frames, dirP), ExceptionMessages.CanNotGetPeriodSize);

        var bufferSize = frames * header.BlockAlign;
        var readBuffer = new byte[(int)bufferSize];

        fixed (byte* buffer = readBuffer)
        {
            while (!_wasDisposed && !cancellationToken.IsCancellationRequested)
            {
                nint rv = InteropAlsa.snd_pcm_readi(_recordingPcm, (IntPtr)buffer, frames);
                if (rv < 0)
                {
                    int rec = InteropAlsa.snd_pcm_recover(_recordingPcm, (int)rv, 0);
                    if (rec < 0)
                    {
                        _log?.LogError("[ALSA ERROR] snd_pcm_recover failed: {Error}", InteropAlsa.StrError(rec));
                        ThrowErrorMessage(rec, ExceptionMessages.CanNotReadFromDevice);
                    }
                    continue;
                }
                onDataAvailable?.Invoke(readBuffer);
            }
        }
    }

    unsafe void PcmInitialize(IntPtr pcm, WavHeader header, ref IntPtr @params, ref int dir)
    {
        ThrowErrorMessage(InteropAlsa.snd_pcm_hw_params_malloc(ref @params), ExceptionMessages.CanNotAllocateParameters);
        ThrowErrorMessage(InteropAlsa.snd_pcm_hw_params_any(pcm, @params), ExceptionMessages.CanNotFillParameters);
        ThrowErrorMessage(InteropAlsa.snd_pcm_hw_params_set_access(pcm, @params, snd_pcm_access_t.SND_PCM_ACCESS_RW_INTERLEAVED), ExceptionMessages.CanNotSetAccessMode);

        // Use probed device capabilities (if available) to request a compatible format/rate/channels
        // similar to how arecord/aplay negotiates hw params. This helps avoid ALSA falling back to
        // undesired low rates (eg. 8000 Hz) when higher-quality formats are available.
        try
        {
            // prefer 16-bit if the device supports it, otherwise use the highest supported bits
            ushort targetBits = Settings.SupportedSampleBits?.Contains((ushort)16) == true ? (ushort)16 : Settings.SupportedSampleBits?.LastOrDefault() ?? Settings.RecordingBitsPerSample;
            if (targetBits == 0) targetBits = 16;

            Core.Native.snd_pcm_format_t targetFmt = Core.Native.snd_pcm_format_t.SND_PCM_FORMAT_UNKNOWN;
            switch (targetBits)
            {
                case 8: targetFmt = Core.Native.snd_pcm_format_t.SND_PCM_FORMAT_U8; break;
                case 16: targetFmt = Core.Native.snd_pcm_format_t.SND_PCM_FORMAT_S16_LE; break;
                case 24: targetFmt = Core.Native.snd_pcm_format_t.SND_PCM_FORMAT_S24_LE; break;
                case 32: targetFmt = Core.Native.snd_pcm_format_t.SND_PCM_FORMAT_S32_LE; break;
                default: targetFmt = Core.Native.snd_pcm_format_t.SND_PCM_FORMAT_S16_LE; break;
            }

            if (targetFmt != Core.Native.snd_pcm_format_t.SND_PCM_FORMAT_UNKNOWN)
            {
                int setf = InteropAlsa.snd_pcm_hw_params_set_format(pcm, @params, targetFmt);
                if (setf == 0)
                {
                    // update bits mapping
                    switch (targetFmt)
                    {
                        case Core.Native.snd_pcm_format_t.SND_PCM_FORMAT_U8: header.BitsPerSample = 8; break;
                        case Core.Native.snd_pcm_format_t.SND_PCM_FORMAT_S16_LE: header.BitsPerSample = 16; break;
                        case Core.Native.snd_pcm_format_t.SND_PCM_FORMAT_S24_LE:
                        case Core.Native.snd_pcm_format_t.SND_PCM_FORMAT_S24_3LE: header.BitsPerSample = 24; break;
                        case Core.Native.snd_pcm_format_t.SND_PCM_FORMAT_S32_LE: header.BitsPerSample = 32; break;
                    }
                }
            }

            // set channels
            uint wantCh = Settings.RecordingChannels != 0 ? Settings.RecordingChannels : (Settings.SupportedChannels?.Any() == true ? Settings.SupportedChannels.First() : 2u);
            int sch = InteropAlsa.snd_pcm_hw_params_set_channels(pcm, @params, wantCh);
            if (sch == 0)
            {
                header.NumChannels = (ushort)wantCh;
            }

            // set rate near preferred or try 48000 first
            uint wantRate = Settings.RecordingSampleRate != 0 ? Settings.RecordingSampleRate : (Settings.SupportedSampleRates?.Contains(48000) == true ? 48000u : (Settings.SupportedSampleRates?.LastOrDefault() ?? 48000u));
            uint rr = wantRate;
            int rdir = dir;
            int rrRv = InteropAlsa.snd_pcm_hw_params_set_rate_near(pcm, @params, &rr, &rdir);
            if (rrRv == 0 && rr != 0)
            {
                header.SampleRate = rr;
                dir = rdir;
            }
        }
        catch (Exception ex)
        {
            _log?.LogDebug(ex, "[ALSA DEBUG] Device probing-based parameter setting failed");
        }

        // Attempt to set hardware params; if we get a transient I/O error (-EIO)
        // try to recover the PCM and retry once before failing.
        int hwRv = InteropAlsa.snd_pcm_hw_params(pcm, @params);
        if (hwRv < 0)
        {
            _log?.LogDebug("[ALSA DEBUG] snd_pcm_hw_params returned {HwRv}, attempting snd_pcm_recover", hwRv);
            int rec = InteropAlsa.snd_pcm_recover(pcm, hwRv, 0);
            if (rec < 0)
            {
                ThrowErrorMessage(rec, ExceptionMessages.CanNotSetHwParams);
            }
            // retry setting hw params after recovery
            hwRv = InteropAlsa.snd_pcm_hw_params(pcm, @params);
            if (hwRv < 0)
            {
                ThrowErrorMessage(hwRv, ExceptionMessages.CanNotSetHwParams);
            }
        }

        // Read back negotiated hw params (format, rate, channels) so callers can
        // correctly interpret captured sample bytes. Do this after snd_pcm_hw_params
        // so we get the actual values ALSA selected.
        try
        {
            Core.Native.snd_pcm_format_t fmt = Core.Native.snd_pcm_format_t.SND_PCM_FORMAT_UNKNOWN;
            uint rate = 0;
            int rdir2 = 0;
            uint channels = 0;
            // Attempt to read format/rate/channels; failures should not be fatal
            _ = InteropAlsa.snd_pcm_hw_params_get_format(@params, &fmt);
            _ = InteropAlsa.snd_pcm_hw_params_get_rate(@params, &rate, &rdir2);
            _ = InteropAlsa.snd_pcm_hw_params_get_channels(@params, &channels);

            // Map ALSA format to bits per sample when possible
            ushort bits = header.BitsPerSample;
            switch (fmt)
            {
                case Core.Native.snd_pcm_format_t.SND_PCM_FORMAT_S16_LE:
                case Core.Native.snd_pcm_format_t.SND_PCM_FORMAT_S16_BE:
                    bits = 16;
                    break;
                case Core.Native.snd_pcm_format_t.SND_PCM_FORMAT_S24_LE:
                case Core.Native.snd_pcm_format_t.SND_PCM_FORMAT_S24_BE:
                case Core.Native.snd_pcm_format_t.SND_PCM_FORMAT_S24_3LE:
                case Core.Native.snd_pcm_format_t.SND_PCM_FORMAT_S24_3BE:
                    bits = 24;
                    break;
                case Core.Native.snd_pcm_format_t.SND_PCM_FORMAT_S32_LE:
                case Core.Native.snd_pcm_format_t.SND_PCM_FORMAT_S32_BE:
                    bits = 32;
                    break;
                case Core.Native.snd_pcm_format_t.SND_PCM_FORMAT_FLOAT_LE:
                case Core.Native.snd_pcm_format_t.SND_PCM_FORMAT_FLOAT_BE:
                    bits = 32;
                    break;
            }

            if (rate != 0) header.SampleRate = rate;
            if (channels != 0) header.NumChannels = (ushort)channels;
            header.BitsPerSample = bits;

            // Sync back to settings for consistency
            Settings.RecordingSampleRate = header.SampleRate;
            Settings.RecordingChannels = header.NumChannels;
            Settings.RecordingBitsPerSample = header.BitsPerSample;

            _log?.LogInformation("[ALSA INFO] Recording opened: device={Device} rate={Rate} bits={Bits} channels={Channels}",
                Settings.RecordingDeviceName, header.SampleRate, header.BitsPerSample, header.NumChannels);
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "[ALSA DEBUG] Could not read negotiated hw params");
        }
    }

    void SetPlaybackVolume(nint volume)
    {
        OpenMixer();

        ThrowErrorMessage(InteropAlsa.snd_mixer_selem_set_playback_volume(_mixerElement, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_LEFT, volume), ExceptionMessages.CanNotSetVolume);
        ThrowErrorMessage(InteropAlsa.snd_mixer_selem_set_playback_volume(_mixerElement, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_RIGHT, volume), ExceptionMessages.CanNotSetVolume);

        CloseMixer();
    }

    unsafe nint GetPlaybackVolume()
    {
        nint volumeLeft = 0;
        nint volumeRight = 0;

        OpenMixer();

        if (InteropAlsa.snd_mixer_selem_has_playback_volume(_mixerElement) == 0)
            throw new AlsaDeviceException(ExceptionMessages.CanNotSetVolume + " - element does not support playback volume");

        ThrowErrorMessage(InteropAlsa.snd_mixer_selem_get_playback_volume(_mixerElement, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_LEFT, &volumeLeft), ExceptionMessages.CanNotSetVolume);
        ThrowErrorMessage(InteropAlsa.snd_mixer_selem_get_playback_volume(_mixerElement, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_RIGHT, &volumeRight), ExceptionMessages.CanNotSetVolume);

        CloseMixer();

        return (volumeLeft + volumeRight) / 2;
    }

    void SetRecordingVolume(nint volume)
    {
        OpenMixer();

        ThrowErrorMessage(InteropAlsa.snd_mixer_selem_set_capture_volume(_mixerElement, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_LEFT, volume), ExceptionMessages.CanNotSetVolume);
        ThrowErrorMessage(InteropAlsa.snd_mixer_selem_set_capture_volume(_mixerElement, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_RIGHT, volume), ExceptionMessages.CanNotSetVolume);

        CloseMixer();
    }

    unsafe nint GetRecordingVolume()
    {
        nint volumeLeft = 0;
        nint volumeRight = 0;

        OpenMixer();

        ThrowErrorMessage(InteropAlsa.snd_mixer_selem_get_capture_volume(_mixerElement, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_LEFT, &volumeLeft), ExceptionMessages.CanNotSetVolume);
        ThrowErrorMessage(InteropAlsa.snd_mixer_selem_get_capture_volume(_mixerElement, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_RIGHT, &volumeRight), ExceptionMessages.CanNotSetVolume);

        CloseMixer();

        return (volumeLeft + volumeRight) / 2;
    }

    void SetPlaybackMute(bool isMute)
    {
        _playbackMute = isMute;

        OpenMixer();

        ThrowErrorMessage(InteropAlsa.snd_mixer_selem_set_playback_switch_all(_mixerElement, isMute ? 0 : 1), ExceptionMessages.CanNotSetMute);

        CloseMixer();
    }

    void SetRecordingMute(bool isMute)
    {
        _recordingMute = isMute;

        OpenMixer();

        ThrowErrorMessage(InteropAlsa.snd_mixer_selem_set_playback_switch_all(_mixerElement, isMute ? 0 : 1), ExceptionMessages.CanNotSetMute);

        CloseMixer();
    }

    void OpenPlaybackPcm()
    {
        if (_playbackPcm != default)
            return;

        lock (PlaybackInitializationLock)
            ThrowErrorMessage(InteropAlsa.snd_pcm_open(ref _playbackPcm, Settings.PlaybackDeviceName, snd_pcm_stream_t.SND_PCM_STREAM_PLAYBACK, 0), ExceptionMessages.CanNotOpenPlayback);
    }

    void ClosePlaybackPcm()
    {
        lock (PlaybackInitializationLock)
        {
            if (_playbackPcm == default)
                return;

            try
            {
                // Use snd_pcm_drop to stop the PCM immediately instead of draining.
                // Draining can block indefinitely with some plugins (eg. pipewire); dropping
                // is faster and avoids hang during shutdown.
                var rc = InteropAlsa.snd_pcm_drop(_playbackPcm);
                if (rc < 0)
                    _log?.LogError("[ALSA ERROR] Can not drop playback device: {Error}", InteropAlsa.StrError(rc));
            }
            catch (Exception ex)
            {
                _log?.LogError(ex, "[ALSA ERROR] Exception during snd_pcm_drop playback: {Message}", ex.Message);
            }

            try
            {
                var rc2 = InteropAlsa.snd_pcm_close(_playbackPcm);
                if (rc2 < 0)
                    _log?.LogError("[ALSA ERROR] Can not close playback device: {Error}", InteropAlsa.StrError(rc2));
            }
            catch (Exception ex)
            {
                _log?.LogError(ex, "[ALSA ERROR] Exception during snd_pcm_close playback: {Message}", ex.Message);
            }

            _playbackPcm = default;
        }
    }

    void OpenRecordingPcm()
    {
        if (_recordingPcm != default)
            return;

        lock (RecordingInitializationLock)
            ThrowErrorMessage(InteropAlsa.snd_pcm_open(ref _recordingPcm, Settings.RecordingDeviceName, snd_pcm_stream_t.SND_PCM_STREAM_CAPTURE, 0), ExceptionMessages.CanNotOpenRecording);
    }

    void CloseRecordingPcm()
    {
        lock (RecordingInitializationLock)
        {
            if (_recordingPcm == default)
                return;

            try
            {
                // Prefer dropping the PCM instead of draining to avoid blocking in
                // plugins like pipewire when shutting down. Drop stops immediately.
                var rc = InteropAlsa.snd_pcm_drop(_recordingPcm);
                if (rc < 0)
                    _log?.LogError("[ALSA ERROR] Can not drop recording device: {Error}", InteropAlsa.StrError(rc));
            }
            catch (Exception ex)
            {
                _log?.LogError(ex, "[ALSA ERROR] Exception during snd_pcm_drop recording: {Message}", ex.Message);
            }

            try
            {
                var rc2 = InteropAlsa.snd_pcm_close(_recordingPcm);
                if (rc2 < 0)
                    _log?.LogError("[ALSA ERROR] Can not close recording device: {Error}", InteropAlsa.StrError(rc2));
            }
            catch (Exception ex)
            {
                _log?.LogError(ex, "[ALSA ERROR] Exception during snd_pcm_close recording: {Message}", ex.Message);
            }

            _recordingPcm = default;
        }
    }

    void OpenMixer()
    {
        if (_mixer != default)
            return;

        lock (MixerInitializationLock)
        {
            IntPtr mix = IntPtr.Zero;
            ThrowErrorMessage(InteropAlsa.snd_mixer_open(out mix, 0), ExceptionMessages.CanNotOpenMixer);
            _mixer = mix;
            ThrowErrorMessage(InteropAlsa.snd_mixer_attach(_mixer, Settings.MixerDeviceName), ExceptionMessages.CanNotAttachMixer);
            ThrowErrorMessage(InteropAlsa.snd_mixer_selem_register(_mixer, IntPtr.Zero, IntPtr.Zero), ExceptionMessages.CanNotRegisterMixer);
            ThrowErrorMessage(InteropAlsa.snd_mixer_load(_mixer), ExceptionMessages.CanNotLoadMixer);

            _mixerElement = InteropAlsa.snd_mixer_first_elem(_mixer);
        }
    }

    void CloseMixer()
    {
        if (_mixer == default)
            return;

        lock (MixerInitializationLock)
        {
            ThrowErrorMessage(InteropAlsa.snd_mixer_close(_mixer), ExceptionMessages.CanNotCloseMixer);

            _mixer = default;
            _mixerElement = default;
        }
    }

    public void Dispose()
    {
        if (_wasDisposed)
            return;

        _wasDisposed = true;

        ClosePlaybackPcm();
        CloseRecordingPcm();
        CloseMixer();
    }

    void ThrowErrorMessage(nint errorNum, string message)
    {
        if (errorNum >= 0)
            return;

        var errno = (int)errorNum;
        var errorMsg = InteropAlsa.StrError(errno);
        _log?.LogError("[ALSA ERROR] {Message}. Error {Errno}. {ErrorMsg}", message, errno, errorMsg);
        throw new AlsaDeviceException($"{message}. Error {errno}. {errorMsg}.");
    }

    public int SetSimpleElementValue(string simpleElementName, string channelName, nint value)
    {
        OpenMixer();
        try
        {
            // iterate mixer simple elements to find matching name
            IntPtr elem = InteropAlsa.snd_mixer_first_elem(_mixer);
            while (elem != IntPtr.Zero)
            {
                try
                {
                    IntPtr namePtr = InteropAlsa.snd_mixer_selem_get_name(elem);
                    var name = Marshal.PtrToStringUTF8(namePtr) ?? string.Empty;
                    if (string.Equals(name, simpleElementName, StringComparison.OrdinalIgnoreCase))
                    {
                        // determine channel target
                        snd_mixer_selem_channel_id ch = snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_LEFT;
                        if (!string.IsNullOrEmpty(channelName) && channelName.IndexOf("right", StringComparison.OrdinalIgnoreCase) >= 0)
                            ch = snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_RIGHT;
                        else if (!string.IsNullOrEmpty(channelName) && (channelName.IndexOf("rear", StringComparison.OrdinalIgnoreCase) >= 0 || channelName.IndexOf("back", StringComparison.OrdinalIgnoreCase) >= 0))
                            ch = snd_mixer_selem_channel_id.SND_MIXER_SCHN_REAR_RIGHT;

                        // Prefer playback volume if available
                        if (InteropAlsa.snd_mixer_selem_has_playback_volume(elem) != 0)
                        {
                            // if channelName suggests both/all, use set_playback_volume_all
                            if (string.IsNullOrEmpty(channelName) || channelName.IndexOf("both", StringComparison.OrdinalIgnoreCase) >= 0 || channelName.IndexOf("all", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return InteropAlsa.snd_mixer_selem_set_playback_volume_all(elem, value);
                            }
                            else
                            {
                                return InteropAlsa.snd_mixer_selem_set_playback_volume(elem, ch, value);
                            }
                        }

                        // Fallback to capture volume if playback not supported
                        if (InteropAlsa.snd_mixer_selem_has_capture_channel(elem, ch) != 0)
                        {
                            if (string.IsNullOrEmpty(channelName) || channelName.IndexOf("both", StringComparison.OrdinalIgnoreCase) >= 0 || channelName.IndexOf("all", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return InteropAlsa.snd_mixer_selem_set_capture_volume_all(elem, value);
                            }
                            else
                            {
                                return InteropAlsa.snd_mixer_selem_set_capture_volume(elem, ch, value);
                            }
                        }

                        // If neither volume API exists, try switch (on/off) if value is 0/1
                        try
                        {
                            int switchVal = value == 0 ? 0 : 1;
                            if (string.IsNullOrEmpty(channelName) || channelName.IndexOf("both", StringComparison.OrdinalIgnoreCase) >= 0 || channelName.IndexOf("all", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                var rvSwitch = InteropAlsa.snd_mixer_selem_set_playback_switch_all(elem, switchVal);
                                if (rvSwitch >= 0)
                                    return rvSwitch;
                                rvSwitch = InteropAlsa.snd_mixer_selem_set_capture_switch_all(elem, switchVal);
                                if (rvSwitch >= 0)
                                    return rvSwitch;
                            }
                            else
                            {
                                var rvSwitch = InteropAlsa.snd_mixer_selem_set_playback_switch(elem, ch, switchVal);
                                if (rvSwitch >= 0)
                                    return rvSwitch;
                                rvSwitch = InteropAlsa.snd_mixer_selem_set_capture_switch(elem, ch, switchVal);
                                if (rvSwitch >= 0)
                                    return rvSwitch;
                            }
                        }
                        catch (Exception ex)
                        {
                            _log?.LogError(ex, "[ALSA ERROR] SetSimpleElementValue switch attempt failed for '{Name}': {Message}", simpleElementName, ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log?.LogError(ex, "[ALSA ERROR] Error iterating mixer elements: {Message}", ex.Message);
                }

                elem = InteropAlsa.snd_mixer_elem_next(elem);
            }

            // fallback: use current stored _mixerElement if available
            if (_mixerElement != IntPtr.Zero)
            {
                if (InteropAlsa.snd_mixer_selem_has_playback_volume(_mixerElement) != 0)
                {
                    return InteropAlsa.snd_mixer_selem_set_playback_volume(_mixerElement, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_LEFT, value);
                }
            }

            return -1; // not found / not supported
        }
        finally
        {
            CloseMixer();
        }
    }

    
}
