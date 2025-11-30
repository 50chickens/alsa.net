using System.Runtime.InteropServices;
using Alsa.Net.Core;

namespace Alsa.Net.Internal
{
    /// <summary>
    /// Gets a list of available ALSA sound cards.
    /// </summary>
    public class AlsaCardEnumerator : IDisposable
    {
        /// <summary>
        /// Implements IDisposable.
        /// </summary>
        public void Dispose()
        {
            // no managed resources
        }

        /// <summary>
        /// Gets a list of all available ALSA sound cards using libasound interop.
        /// </summary>
        /// <returns>An array of discovered <see cref="Card"/> instances.</returns>
        public IEnumerable<Card> GetCards()
        {
            var list = new List<Card>();
            int card = -1;

            // Start enumeration
            var rc = InteropAlsa.snd_card_next(ref card);
            if (rc < 0)
                throw new InvalidOperationException($"snd_card_next failed: {InteropAlsa.StrError(rc)}");

            while (card >= 0)
            {
                // Obtain the short name for the card via the proper libasound API.
                IntPtr namePtr = IntPtr.Zero;
                rc = InteropAlsa.snd_card_get_name(card, out namePtr);
                if (rc < 0)
                    throw new InvalidOperationException($"snd_card_get_name({card}) failed: {InteropAlsa.StrError(rc)}");

                    try
                    {
                        string name = Marshal.PtrToStringUTF8(namePtr) ?? string.Empty;
                        list.Add(new Card(LogManager.GetLogger<Card>(), card, name));
                    }
                finally
                {
                    // free memory allocated by the ALSA helper
                    if (namePtr != IntPtr.Zero)
                        InteropAlsa.free(namePtr);
                }

                rc = InteropAlsa.snd_card_next(ref card);
                if (rc < 0)
                    throw new InvalidOperationException($"snd_card_next failed: {InteropAlsa.StrError(rc)}");
            }

            return list.ToArray();
        }
    }
}


