using System;
using AlsaSharp;
#nullable enable

namespace Example.SNRReduction.Services
{
    /// <summary>
    /// Small helper service that exposes audio card configuration helpers used by the example.
    /// Keeps example-specific logic here while low-level mixer operations live in AlsaSharp.
    /// </summary>
    public class AudioCardConfigService
    {
        /// <summary>
        /// Attempts to set the 'DAI Left Source MUX' to a preferred label (tries the provided label).
        /// Returns true when the operation succeeded.
        /// </summary>
        public bool SetDaiLeftSourceMux(int cardIndex, string label)
        {
            try
            {
                return MixerManager.SetEnumItemByLabel(cardIndex, "DAI Left Source MUX", label);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Try a small set of preferred labels and return the first that succeeds, or null.
        /// </summary>
        public string? TryPreferredDaiMux(int cardIndex)
        {
            var candidates = new[] { "ADC Right", "ADC Left" };
            foreach (var c in candidates)
            {
                if (SetDaiLeftSourceMux(cardIndex, c))
                    return c;
            }
            return null;
        }
    }
}
