using AlsaSharp.Core.Native;
using AlsaSharp.Library.Logging;

namespace AlsaSharp.Library;

/// <summary>
/// Handles ALSA mixer and volume control operations.
/// </summary>
internal class AlsaMixerDevice : IDisposable
{
    private static readonly object MixerLock = new();
    
    private readonly ILog<UnixSoundDevice>? _log;
    private readonly SoundDeviceSettings _settings;
    
    private IntPtr _mixer;
    private IntPtr _mixerElement;
    private bool _playbackMute;
    private bool _recordingMute;
    private bool _disposed;
    private bool disposedValue;

    public AlsaMixerDevice(SoundDeviceSettings settings, ILog<UnixSoundDevice>? log = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _log = log;
    }

    public bool PlaybackMute
    {
        get => _playbackMute;
        set => SetPlaybackMute(value);
    }

    public bool RecordingMute
    {
        get => _recordingMute;
        set => SetRecordingMute(value);
    }

    public long GetPlaybackVolume()
    {
        return NativeWidth.FromNint(GetPlaybackVolumeInternal());
    }

    public void SetPlaybackVolume(long volume)
    {
        SetPlaybackVolumeInternal(NativeWidth.ToNint(volume));
    }

    public long GetRecordingVolume()
    {
        return NativeWidth.FromNint(GetRecordingVolumeInternal());
    }

    public void SetRecordingVolume(long volume)
    {
        SetRecordingVolumeInternal(NativeWidth.ToNint(volume));
    }

    private void SetPlaybackVolumeInternal(nint volume)
    {
        OpenMixer();
        try
        {
            AlsaErrorHandler.ValidateResult(
                InteropAlsa.snd_mixer_selem_set_playback_volume(_mixerElement, 
                    snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_LEFT, volume),
}
                ExceptionMessages.CanNotSetVolume, _log);
            
            AlsaErrorHandler.ValidateResult(
                InteropAlsa.snd_mixer_selem_set_playback_volume(_mixerElement, 
                    snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_RIGHT, volume),
                ExceptionMessages.CanNotSetVolume, _log);
        }
        finally
        {
            CloseMixer();
        }
    }

    private unsafe nint GetPlaybackVolumeInternal()
    {
        nint volumeLeft = 0;
        nint volumeRight = 0;

        OpenMixer();
        try
        {
            if (InteropAlsa.snd_mixer_selem_has_playback_volume(_mixerElement) == 0)
            {
                throw new AlsaDeviceException(
                    ExceptionMessages.CanNotSetVolume + " - element does not support playback volume");
            }

            AlsaErrorHandler.ValidateResult(
                InteropAlsa.snd_mixer_selem_get_playback_volume(_mixerElement, 
                    snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_LEFT, &volumeLeft),
                ExceptionMessages.CanNotSetVolume, _log);
            
            AlsaErrorHandler.ValidateResult(
                InteropAlsa.snd_mixer_selem_get_playback_volume(_mixerElement, 
                    snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_RIGHT, &volumeRight),
                ExceptionMessages.CanNotSetVolume, _log);

            return (volumeLeft + volumeRight) / 2;
        }
        finally
        {
            CloseMixer();
        }
    }

    private void SetRecordingVolumeInternal(nint volume)
    {
        OpenMixer();
        try
        {
            AlsaErrorHandler.ValidateResult(
                InteropAlsa.snd_mixer_selem_set_capture_volume(_mixerElement, 
                    snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_LEFT, volume),
                ExceptionMessages.CanNotSetVolume, _log);
            
            AlsaErrorHandler.ValidateResult(
                InteropAlsa.snd_mixer_selem_set_capture_volume(_mixerElement, 
                    snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_RIGHT, volume),
                ExceptionMessages.CanNotSetVolume, _log);
        }
        finally
        {
            CloseMixer();
        }
    }

    private unsafe nint GetRecordingVolumeInternal()
    {
        nint volumeLeft = 0;
        nint volumeRight = 0;

        OpenMixer();
        try
        {
            AlsaErrorHandler.ValidateResult(
                InteropAlsa.snd_mixer_selem_get_capture_volume(_mixerElement, 
                    snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_LEFT, &volumeLeft),
                ExceptionMessages.CanNotSetVolume, _log);
            
            AlsaErrorHandler.ValidateResult(
                InteropAlsa.snd_mixer_selem_get_capture_volume(_mixerElement, 
                    snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_RIGHT, &volumeRight),
                ExceptionMessages.CanNotSetVolume, _log);

            return (volumeLeft + volumeRight) / 2;
        }
        finally
        {
            CloseMixer();
        }
    }

    private void SetPlaybackMute(bool isMute)
    {
        _playbackMute = isMute;
        
        OpenMixer();
        try
        {
            AlsaErrorHandler.ValidateResult(
                InteropAlsa.snd_mixer_selem_set_playback_switch_all(_mixerElement, isMute ? 0 : 1),
                ExceptionMessages.CanNotSetMute, _log);
        }
        finally
        {
            CloseMixer();
        }
    }

    private void SetRecordingMute(bool isMute)
    {
        _recordingMute = isMute;
        
        OpenMixer();
        try
        {
            AlsaErrorHandler.ValidateResult(
                InteropAlsa.snd_mixer_selem_set_playback_switch_all(_mixerElement, isMute ? 0 : 1),
                ExceptionMessages.CanNotSetMute, _log);
        }
        finally
        {
            CloseMixer();
        }
    }

    public int SetSimpleElementValue(string simpleElementName, string channelName, nint value)
    {
        OpenMixer();
        try
        {
            IntPtr element = FindMixerElement(simpleElementName);
            if (element == IntPtr.Zero)
            {
                return TrySetFallbackElement(value);
            }

            var channel = ParseChannelName(channelName);
            return SetElementValue(element, channelName, channel, value);
        }
        finally
        {
            CloseMixer();
        }
    }

    private IntPtr FindMixerElement(string targetName)
    {
        IntPtr element = InteropAlsa.snd_mixer_first_elem(_mixer);
        
        while (element != IntPtr.Zero)
        {
            try
            {
                IntPtr namePtr = InteropAlsa.snd_mixer_selem_get_name(element);
                string? name = Marshal.PtrToStringUTF8(namePtr);
                
                if (string.Equals(name, targetName, StringComparison.OrdinalIgnoreCase))
                {
                    return element;
                }
            }
            catch (Exception ex)
            {
                _log?.Error(m => m("[ALSA] Error reading mixer element name"), ex);
            }

            element = InteropAlsa.snd_mixer_elem_next(element);
        }

        return IntPtr.Zero;
    }

    private snd_mixer_selem_channel_id ParseChannelName(string? channelName)
    {
        if (string.IsNullOrEmpty(channelName))
            return snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_LEFT;

        if (channelName.Contains("right", StringComparison.OrdinalIgnoreCase))
            return snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_RIGHT;
        
        if (channelName.Contains("rear", StringComparison.OrdinalIgnoreCase) || 
            channelName.Contains("back", StringComparison.OrdinalIgnoreCase))
            return snd_mixer_selem_channel_id.SND_MIXER_SCHN_REAR_RIGHT;

        return snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_LEFT;
    }

    private bool IsAllChannels(string? channelName)
    {
        return string.IsNullOrEmpty(channelName) ||
               channelName.Contains("both", StringComparison.OrdinalIgnoreCase) ||
               channelName.Contains("all", StringComparison.OrdinalIgnoreCase);
    }

    private int SetElementValue(IntPtr element, string? channelName, 
        snd_mixer_selem_channel_id channel, nint value)
    {
        if (InteropAlsa.snd_mixer_selem_has_playback_volume(element) != 0)
        {
            return SetPlaybackValue(element, channelName, channel, value);
        }

        if (InteropAlsa.snd_mixer_selem_has_capture_channel(element, channel) != 0)
        {
            return SetCaptureValue(element, channelName, channel, value);
        }

        return TrySetSwitch(element, channelName, channel, value);
    }

    private int SetPlaybackValue(IntPtr element, string? channelName, 
        snd_mixer_selem_channel_id channel, nint value)
    {
        if (IsAllChannels(channelName))
        {
            return InteropAlsa.snd_mixer_selem_set_playback_volume_all(element, value);
        }
        
        return InteropAlsa.snd_mixer_selem_set_playback_volume(element, channel, value);
    }

    private int SetCaptureValue(IntPtr element, string? channelName, 
        snd_mixer_selem_channel_id channel, nint value)
    {
        if (IsAllChannels(channelName))
        {
            return InteropAlsa.snd_mixer_selem_set_capture_volume_all(element, value);
        }
        
        return InteropAlsa.snd_mixer_selem_set_capture_volume(element, channel, value);
    }

    private int TrySetSwitch(IntPtr element, string? channelName, 
        snd_mixer_selem_channel_id channel, nint value)
    {
        try
        {
            int switchValue = value == 0 ? 0 : 1;
            
            if (IsAllChannels(channelName))
            {
