using System.Runtime.InteropServices;
using Alsa.Net.Core;

namespace Alsa.Net.Internal
{
    /// <summary>
    /// Gets a list of available ALSA sound cards.
    /// </summary>
    public class AlsaCardEnumerator : IDisposable
    {
        private IEnumerable<Card> _cards;

        public AlsaCardEnumerator()
        {
            GetCards();
        }
        /// <summary>
        /// Implements IDisposable.
        /// </summary>
        public void Dispose()
        {
            // no managed resources
        }

        /// <summary>
        /// Gets a list of all available ALSA sound cards using libasound interop.
        /// 
        /// </summary>
        /// <returns>An array of discovered <see cref="Card"/> instances.</returns>
        public IEnumerable<Card> GetCards()
        {
            var list = new List<Card>();
            int cardIndex = -1;

            // Start enumeration
            var returnCode = InteropAlsa.snd_card_next(ref cardIndex);
            if (returnCode < 0)
                throw new InvalidOperationException($"snd_card_next failed: {InteropAlsa.StrError(returnCode)}");

            while (cardIndex >= 0)
            {
                // Obtain the short name for the card via the proper libasound API.
                IntPtr namePtr = IntPtr.Zero;
                returnCode = InteropAlsa.snd_card_get_name(cardIndex, out namePtr);
                if (returnCode < 0)
                    throw new InvalidOperationException($"snd_card_get_name({cardIndex}) failed: {InteropAlsa.StrError(returnCode)}");

                    try
                    {
                        string name = Marshal.PtrToStringUTF8(namePtr) ?? string.Empty;
                        list.Add(new Card(LogManager.GetLogger<Card>(), cardIndex, name));
                    }
                finally
                {
                    // free memory allocated by the ALSA helper
                    if (namePtr != IntPtr.Zero)
                        InteropAlsa.free(namePtr);
                }

                returnCode = InteropAlsa.snd_card_next(ref cardIndex);
                if (returnCode < 0)
                    throw new InvalidOperationException($"snd_card_next failed: {InteropAlsa.StrError(returnCode)}");
            }

            return list.ToArray();
        }
        public bool TryGetCards(out IEnumerable<Card> cards)
        {
            cards = new AlsaCardEnumerator().GetCards();
            return cards.ToList().Count != 0 ? true : false;
        }
    }
}


