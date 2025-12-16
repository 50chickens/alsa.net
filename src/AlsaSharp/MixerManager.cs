using System;
using AlsaSharp.Library.Services;

namespace AlsaSharp
{
    /// <summary>
    /// High-level convenience APIs for common mixer operations.
    /// </summary>
    public static class MixerManager
    {
        /// <summary>
        /// Sets an enumerated control item on the given ALSA card by matching the human label.
        /// Returns true when the control was updated.
        /// This is a thin, safe wrapper around the library MixerService.
        /// </summary>
        public static bool SetEnumItemByLabel(int cardIndex, string controlName, string itemLabel, string? channelName = null)
        {
            try
            {
                var svc = new MixerService();
                return svc.TrySetEnumItemByLabel(cardIndex, controlName, itemLabel, channelName);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Reads a mixer element's current value in a generic fashion. Returns tuple(success,type,value).
        /// </summary>
        public static (bool Success, string Type, string Value) GetElementValue(int cardIndex, string controlName)
        {
            try
            {
                var svc = new MixerService();
                if (svc.TryGetElementValue(cardIndex, controlName, out var type, out var value))
                    return (true, type, value);
            }
            catch { }
            return (false, "unknown", string.Empty);
        }
    }
}
