
using System.Runtime.InteropServices;

namespace AlsaSharp.Internal;

class UnixSoundDevice(SoundDeviceSettings settings) : ISoundDevice
{
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
            Console.Error.WriteLine($"[ALSA DEBUG] snd_pcm_hw_params_get_period_size -> {rv}, frames={frames}");
            ThrowErrorMessage(rv, ExceptionMessages.CanNotGetPeriodSize);
        }

        var bufferSize = frames * header.BlockAlign;
        var readBuffer = new byte[(int)bufferSize];

        fixed (byte* buffer = readBuffer)
        {
            while (!_wasDisposed && !cancellationToken.IsCancellationRequested && wavStream.Read(readBuffer) != 0)
            {
                nint rv = InteropAlsa.snd_pcm_writei(_playbackPcm, (IntPtr)buffer, frames);
                Console.Error.WriteLine($"[ALSA DEBUG] snd_pcm_writei -> {rv} (frames requested={frames})");
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
            Console.Error.WriteLine($"[ALSA DEBUG] snd_pcm_hw_params_get_period_size -> {rv}, frames={frames}");
            ThrowErrorMessage(rv, ExceptionMessages.CanNotGetPeriodSize);
        }

        var bufferSize = frames * header.BlockAlign;
        var readBuffer = new byte[(int)bufferSize];

        fixed (byte* buffer = readBuffer)
        {
            while (!_wasDisposed && !cancellationToken.IsCancellationRequested)
            {
                nint rv = InteropAlsa.snd_pcm_readi(_recordingPcm, (IntPtr)buffer, frames);
                Console.Error.WriteLine($"[ALSA DEBUG] snd_pcm_readi -> {rv} (frames requested={frames})");
                if (rv < 0)
                {
                    // try to recover from transient I/O errors (eg. -EIO)
                    int rec = InteropAlsa.snd_pcm_recover(_recordingPcm, (int)rv, 0);
                    if (rec < 0)
                    {
                        Console.Error.WriteLine($"[ALSA ERROR] snd_pcm_recover failed: {InteropAlsa.StrError(rec)}");
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
                        Console.Error.WriteLine($"[ALSA ERROR] snd_pcm_recover failed: {InteropAlsa.StrError(rec)}");
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

        var formatResult = (header.BitsPerSample / 8) switch
        {
            1 => InteropAlsa.snd_pcm_hw_params_set_format(pcm, @params, snd_pcm_format_t.SND_PCM_FORMAT_U8),
            2 => InteropAlsa.snd_pcm_hw_params_set_format(pcm, @params, snd_pcm_format_t.SND_PCM_FORMAT_S16_LE),
            3 => InteropAlsa.snd_pcm_hw_params_set_format(pcm, @params, snd_pcm_format_t.SND_PCM_FORMAT_S24_LE),
            4 => InteropAlsa.snd_pcm_hw_params_set_format(pcm, @params, snd_pcm_format_t.SND_PCM_FORMAT_S32_LE),
            _ => throw new AlsaDeviceException(ExceptionMessages.BitsPerSampleError)
        };
        ThrowErrorMessage(formatResult, ExceptionMessages.CanNotSetFormat);

        ThrowErrorMessage(InteropAlsa.snd_pcm_hw_params_set_channels(pcm, @params, header.NumChannels), ExceptionMessages.CanNotSetChannel);

        var val = header.SampleRate;
        fixed (int* dirP = &dir)
            ThrowErrorMessage(InteropAlsa.snd_pcm_hw_params_set_rate_near(pcm, @params, &val, dirP), ExceptionMessages.CanNotSetRate);

        // Attempt to set hardware params; if we get a transient I/O error (-EIO)
        // try to recover the PCM and retry once before failing.
        int hwRv = InteropAlsa.snd_pcm_hw_params(pcm, @params);
        if (hwRv < 0)
        {
            Console.Error.WriteLine($"[ALSA DEBUG] snd_pcm_hw_params returned {hwRv}, attempting snd_pcm_recover");
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
                if (rc < 0) Console.Error.WriteLine($"[ALSA ERROR] Can not drop playback device: {InteropAlsa.StrError(rc)}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ALSA ERROR] Exception during snd_pcm_drop playback: {ex.Message}");
            }

            try
            {
                var rc2 = InteropAlsa.snd_pcm_close(_playbackPcm);
                if (rc2 < 0) Console.Error.WriteLine($"[ALSA ERROR] Can not close playback device: {InteropAlsa.StrError(rc2)}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ALSA ERROR] Exception during snd_pcm_close playback: {ex.Message}");
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
                if (rc < 0) Console.Error.WriteLine($"[ALSA ERROR] Can not drop recording device: {InteropAlsa.StrError(rc)}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ALSA ERROR] Exception during snd_pcm_drop recording: {ex.Message}");
            }

            try
            {
                var rc2 = InteropAlsa.snd_pcm_close(_recordingPcm);
                if (rc2 < 0) Console.Error.WriteLine($"[ALSA ERROR] Can not close recording device: {InteropAlsa.StrError(rc2)}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ALSA ERROR] Exception during snd_pcm_close recording: {ex.Message}");
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

    static void ThrowErrorMessage(nint errorNum, string message)
    {
        if (errorNum >= 0)
            return;

        var errno = (int)errorNum;
        var errorMsg = InteropAlsa.StrError(errno);
        Console.Error.WriteLine($"[ALSA ERROR] {message}. Error {errno}. {errorMsg}");
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
                                if (rvSwitch >= 0) return rvSwitch;
                                rvSwitch = InteropAlsa.snd_mixer_selem_set_capture_switch_all(elem, switchVal);
                                if (rvSwitch >= 0) return rvSwitch;
                            }
                            else
                            {
                                var rvSwitch = InteropAlsa.snd_mixer_selem_set_playback_switch(elem, ch, switchVal);
                                if (rvSwitch >= 0) return rvSwitch;
                                rvSwitch = InteropAlsa.snd_mixer_selem_set_capture_switch(elem, ch, switchVal);
                                if (rvSwitch >= 0) return rvSwitch;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"[ALSA ERROR] SetSimpleElementValue switch attempt failed for '{simpleElementName}': {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ALSA ERROR] Error iterating mixer elements: {ex.Message}");
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

    public void RestoreStateFromAlsactlFile(string stateFilePath)
    {
        if (string.IsNullOrEmpty(stateFilePath) || !File.Exists(stateFilePath))
        {
            Console.Error.WriteLine($"[ALSA INFO] State file not found: {stateFilePath}");
            return;
        }

        string[] lines = File.ReadAllLines(stateFilePath);
        string currentName = null;
        // simple parser: when we find a name '...' line, collect subsequent value lines until the next control or closing brace
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.StartsWith("name "))
            {
                // name 'Mic 1 Volume'
                int firstQuote = line.IndexOf('\'');
                int lastQuote = line.LastIndexOf('\'');
                if (firstQuote >= 0 && lastQuote > firstQuote)
                {
                    currentName = line.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
                }
                else
                {
                    // fallback: split
                    var parts = line.Split(new[] { ' ' }, 2);
                    currentName = parts.Length > 1 ? parts[1].Trim().Trim('"') : null;
                }
            }

            if (currentName != null && line.StartsWith("value"))
            {
                // value, value.0, value.1 formats
                // examples: value 0  OR value.0 53  OR value false
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    string key = parts[0];
                    string rawVal = parts[1];
                    // if more tokens exist, last token is value
                    if (parts.Length > 2) rawVal = parts[parts.Length - 1];

                    // try boolean
                    if (string.Equals(rawVal, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(rawVal, "false", StringComparison.OrdinalIgnoreCase))
                    {
                        int v = string.Equals(rawVal, "true", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                        // channel specified?
                        if (key.Contains('.'))
                        {
                            var idxPart = key.Split('.').Last();
                            if (int.TryParse(idxPart, out int idx) && idx >= 0)
                            {
                                var ch = idx == 0 ? "left" : (idx == 1 ? "right" : "");
                                try { SetSimpleElementValue(currentName, ch, v); } catch (Exception ex) { Console.Error.WriteLine($"[ALSA ERROR] RestoreState: failed to set {currentName}:{ch} -> {v}: {ex.Message}"); }
                            }
                        }
                        else
                        {
                            try { SetSimpleElementValue(currentName, string.Empty, v); } catch (Exception ex) { Console.Error.WriteLine($"[ALSA ERROR] RestoreState: failed to set {currentName} -> {v}: {ex.Message}"); }
                        }
                        continue;
                    }

                    // try integer
                    if (int.TryParse(rawVal, out int intVal))
                    {
                        if (key.Contains('.'))
                        {
                            var idxPart = key.Split('.').Last();
                            if (int.TryParse(idxPart, out int idx) && idx >= 0)
                            {
                                var ch = idx == 0 ? "left" : (idx == 1 ? "right" : "");
                                try { SetSimpleElementValue(currentName, ch, intVal); } catch (Exception ex) { Console.Error.WriteLine($"[ALSA ERROR] RestoreState: failed to set {currentName}:{ch} -> {intVal}: {ex.Message}"); }
                            }
                        }
                        else
                        {
                            try { SetSimpleElementValue(currentName, string.Empty, intVal); } catch (Exception ex) { Console.Error.WriteLine($"[ALSA ERROR] RestoreState: failed to set {currentName} -> {intVal}: {ex.Message}"); }
                        }
                        continue;
                    }

                    // could not parse (enum or string) - skip
                    Console.Error.WriteLine($"[ALSA INFO] RestoreState: skipping unsupported value for control '{currentName}': {rawVal}");
                }
            }

            // reset on end of control block
            if (line == "}")
            {
                currentName = null;
            }
        }
    }
}