using AlsaSharp.Core.Native;
using AlsaSharp.Library.Logging;

namespace AlsaSharp.Library;

/// <summary>
/// Handles PCM device initialization and hardware parameter configuration.
/// </summary>
internal class AlsaPcmInitializer
{
    private readonly ILog<UnixSoundDevice>? _log;
    private readonly SoundDeviceSettings _settings;

    public AlsaPcmInitializer(SoundDeviceSettings settings, ILog<UnixSoundDevice>? log = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _log = log;
    }

    public unsafe void InitializePcm(IntPtr pcm, WavHeader header, ref IntPtr @params, ref int dir)
    {
        AllocateAndFillParameters(pcm, ref @params);
        ConfigureHardwareParameters(pcm, header, ref @params, ref dir);
        ApplyHardwareParameters(pcm, @params);
        ReadNegotiatedParameters(pcm, @params, header);
    }

    private void AllocateAndFillParameters(IntPtr pcm, ref IntPtr @params)
    {
        AlsaErrorHandler.ValidateResult(
            InteropAlsa.snd_pcm_hw_params_malloc(ref @params), 
            ExceptionMessages.CanNotAllocateParameters, _log);
        
        AlsaErrorHandler.ValidateResult(
            InteropAlsa.snd_pcm_hw_params_any(pcm, @params), 
            ExceptionMessages.CanNotFillParameters, _log);
        
        AlsaErrorHandler.ValidateResult(
            InteropAlsa.snd_pcm_hw_params_set_access(pcm, @params, 
                snd_pcm_access_t.SND_PCM_ACCESS_RW_INTERLEAVED), 
            ExceptionMessages.CanNotSetAccessMode, _log);
    }

    private unsafe void ConfigureHardwareParameters(IntPtr pcm, WavHeader header, 
        ref IntPtr @params, ref int dir)
    {
        try
        {
            SetOptimalFormat(pcm, @params, header);
            SetOptimalChannels(pcm, @params, header);
            SetOptimalSampleRate(pcm, @params, header, ref dir);
        }
        catch (Exception ex)
        {
            _log?.Error(ex, "[ALSA] Device capability probing failed, using defaults");
        }
    }

    private unsafe void SetOptimalFormat(IntPtr pcm, IntPtr @params, WavHeader header)
    {
        var targetBits = DetermineTargetBits();
        var targetFormat = MapBitsToFormat(targetBits);

        if (targetFormat != snd_pcm_format_t.SND_PCM_FORMAT_UNKNOWN)
        {
            int result = InteropAlsa.snd_pcm_hw_params_set_format(pcm, @params, targetFormat);
            if (result == 0)
            {
                header.BitsPerSample = GetBitsFromFormat(targetFormat);
            }
        }
    }

    private ushort DetermineTargetBits()
    {
        if (_settings.SupportedSampleBits?.Contains((ushort)16) == true)
            return 16;
        
        return _settings.SupportedSampleBits?.LastOrDefault() ?? 
               _settings.RecordingBitsPerSample;
    }

    private snd_pcm_format_t MapBitsToFormat(ushort bits)
    {
        return bits switch
        {
            8 => snd_pcm_format_t.SND_PCM_FORMAT_U8,
            16 => snd_pcm_format_t.SND_PCM_FORMAT_S16_LE,
            24 => snd_pcm_format_t.SND_PCM_FORMAT_S24_LE,
            32 => snd_pcm_format_t.SND_PCM_FORMAT_S32_LE,
            _ => snd_pcm_format_t.SND_PCM_FORMAT_S16_LE
        };
    }

    private ushort GetBitsFromFormat(snd_pcm_format_t format)
    {
        return format switch
        {
            snd_pcm_format_t.SND_PCM_FORMAT_U8 => 8,
            snd_pcm_format_t.SND_PCM_FORMAT_S16_LE => 16,
            snd_pcm_format_t.SND_PCM_FORMAT_S24_LE or 
            snd_pcm_format_t.SND_PCM_FORMAT_S24_3LE => 24,
            snd_pcm_format_t.SND_PCM_FORMAT_S32_LE => 32,
            _ => 16
        };
    }

    private void SetOptimalChannels(IntPtr pcm, IntPtr @params, WavHeader header)
    {
        uint targetChannels = DetermineTargetChannels();
        int result = InteropAlsa.snd_pcm_hw_params_set_channels(pcm, @params, targetChannels);
        
        if (result == 0)
        {
            header.NumChannels = (ushort)targetChannels;
        }
    }

    private uint DetermineTargetChannels()
    {
        if (_settings.RecordingChannels != 0)
            return _settings.RecordingChannels;
        
        if (_settings.SupportedChannels?.Any() == true)
            return _settings.SupportedChannels.First();
        
        return 2u;
    }

    private unsafe void SetOptimalSampleRate(IntPtr pcm, IntPtr @params, 
        WavHeader header, ref int dir)
    {
        uint targetRate = DetermineTargetSampleRate();
        uint actualRate = targetRate;
        int direction = dir;

        int result = InteropAlsa.snd_pcm_hw_params_set_rate_near(pcm, @params, 
            &actualRate, &direction);
        
        if (result == 0 && actualRate != 0)
        {
            header.SampleRate = actualRate;
            dir = direction;
        }
    }

    private uint DetermineTargetSampleRate()
    {
        if (_settings.RecordingSampleRate != 0)
            return _settings.RecordingSampleRate;
        
        if (_settings.SupportedSampleRates?.Contains(48000) == true)
            return 48000u;
        
        return _settings.SupportedSampleRates?.LastOrDefault() ?? 48000u;
    }

    private void ApplyHardwareParameters(IntPtr pcm, IntPtr @params)
    {
        int result = InteropAlsa.snd_pcm_hw_params(pcm, @params);
        
        if (result < 0)
        {
            _log?.Debug($"[ALSA] Initial hw_params failed ({result}), attempting recovery");
            
            int recovery = InteropAlsa.snd_pcm_recover(pcm, result, 0);
            AlsaErrorHandler.ValidateResult(recovery, ExceptionMessages.CanNotSetHwParams, _log);

            result = InteropAlsa.snd_pcm_hw_params(pcm, @params);
            AlsaErrorHandler.ValidateResult(result, ExceptionMessages.CanNotSetHwParams, _log);
        }
    }

    private unsafe void ReadNegotiatedParameters(IntPtr pcm, IntPtr @params, WavHeader header)
    {
        try
        {
            var format = snd_pcm_format_t.SND_PCM_FORMAT_UNKNOWN;
            uint rate = 0;
            int direction = 0;
            uint channels = 0;

            InteropAlsa.snd_pcm_hw_params_get_format(@params, &format);
            InteropAlsa.snd_pcm_hw_params_get_rate(@params, &rate, &direction);
            InteropAlsa.snd_pcm_hw_params_get_channels(@params, &channels);

            ushort bits = MapFormatToBits(format, header.BitsPerSample);

            if (rate != 0) header.SampleRate = rate;
            if (channels != 0) header.NumChannels = (ushort)channels;
            header.BitsPerSample = bits;

            SyncSettingsWithHeader(header);

            _log?.Info($"[ALSA] Device opened: {_settings.RecordingDeviceName} rate={header.SampleRate}Hz bits={header.BitsPerSample} channels={header.NumChannels}");
 
        }
        catch (Exception ex)
        {
            _log?.Error(ex, "[ALSA] Failed to read negotiated hardware parameters");
            throw;
        }
    }

    private ushort MapFormatToBits(snd_pcm_format_t format, ushort defaultBits)
    {
        return format switch
        {
            snd_pcm_format_t.SND_PCM_FORMAT_S16_LE or 
            snd_pcm_format_t.SND_PCM_FORMAT_S16_BE => 16,
            
            snd_pcm_format_t.SND_PCM_FORMAT_S24_LE or 
            snd_pcm_format_t.SND_PCM_FORMAT_S24_BE or
            snd_pcm_format_t.SND_PCM_FORMAT_S24_3LE or 
            snd_pcm_format_t.SND_PCM_FORMAT_S24_3BE => 24,
            
            snd_pcm_format_t.SND_PCM_FORMAT_S32_LE or 
            snd_pcm_format_t.SND_PCM_FORMAT_S32_BE or
            snd_pcm_format_t.SND_PCM_FORMAT_FLOAT_LE or 
            snd_pcm_format_t.SND_PCM_FORMAT_FLOAT_BE => 32,
            
            _ => defaultBits
        };
    }

    private void SyncSettingsWithHeader(WavHeader header)
    {
        _settings.RecordingSampleRate = header.SampleRate;
        _settings.RecordingChannels = header.NumChannels;
        _settings.RecordingBitsPerSample = header.BitsPerSample;
    }
}
