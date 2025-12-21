using AlsaSharp.Core.Native;
using AlsaSharp.Library.Logging;

namespace AlsaSharp.Library;

/// <summary>
/// Provides helper utilities for ALSA PCM operations.
/// </summary>
internal static class AlsaPcmHelper
{
    public static unsafe nuint GetPeriodSize(IntPtr @params, ref int dir, ILog<UnixSoundDevice>? log)
    {
        nuint frames;
        fixed (int* dirPtr = &dir)
        {
            int result = InteropAlsa.snd_pcm_hw_params_get_period_size(@params, &frames, dirPtr);
            log?.Trace($"[ALSA] Period size: {frames} frames");
            AlsaErrorHandler.ValidateResult(result, ExceptionMessages.CanNotGetPeriodSize, log);
        }
        return frames;
    }

    public static void DropAndClosePcm(IntPtr pcm, string deviceType, ILog<UnixSoundDevice>? log)
    {
        try
        {
            int result = InteropAlsa.snd_pcm_drop(pcm);
            if (result < 0)
            {
                log?.Error($"[ALSA] Failed to drop {deviceType} device: {InteropAlsa.StrError(result)}");
            }
        }
        catch (Exception ex)
        {
            log?.Error(ex, $"[ALSA] Exception during snd_pcm_drop ({deviceType})");
            throw;
        }

        try
        {
            int result = InteropAlsa.snd_pcm_close(pcm);
            if (result < 0)
            {
                log?.Error($"[ALSA] Failed to close {deviceType} device: {InteropAlsa.StrError(result)}");
            }
        }
        catch (Exception ex)
        {
            log?.Error(ex, $"[ALSA] Exception during snd_pcm_close ({deviceType})");
            throw;
        }
    }
}
