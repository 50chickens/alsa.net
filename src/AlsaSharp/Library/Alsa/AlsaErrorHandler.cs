
using AlsaSharp.Core.Native;
using AlsaSharp.Library.Logging;

namespace AlsaSharp.Library;

/// <summary>
/// Handles ALSA error validation and recovery.
/// </summary>
internal static class AlsaErrorHandler
{
    public static bool RecoverFromError(IntPtr pcm, int errorCode, ILog<UnixSoundDevice>? log)
    {
        int recovery = InteropAlsa.snd_pcm_recover(pcm, errorCode, 0);
        
        if (recovery < 0)
        {
            log?.Error($"[ALSA] Recovery failed: {InteropAlsa.StrError(recovery)}");
            return false;
        }

        return true;
    }

    public static void ValidateResult(nint result, string errorMessage, ILog<UnixSoundDevice>? log)
    {
        if (result >= 0)
            return;

        int errorCode = (int)result;
        string alsaError = InteropAlsa.StrError(errorCode);

        log?.Error($"[ALSA] {errorMessage}. Code: {errorCode}, Details: {alsaError}");

        throw new AlsaDeviceException($"{errorMessage}. Error {errorCode}: {alsaError}");
    }
}
