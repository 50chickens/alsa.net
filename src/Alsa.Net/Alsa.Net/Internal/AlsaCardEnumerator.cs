using System.Runtime.InteropServices;

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
            // Clean up resources here
        }

        /// <summary>
        /// Gets a list of all available ALSA sound cards.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Card> GetCards()
        {
            try
            {
                var list = new List<Card>();
                int card = -1;
                int returnCode = InteropAlsa.snd_card_next(ref card);
                if (returnCode < 0) return Array.Empty<Card>();

                while (card >= 0)
                {
                    string name = Marshal.PtrToStringUTF8(InteropAlsa.snd_card_get_name(card)) ?? string.Empty;
                    list.Add(new Card(card, name));
                    returnCode = InteropAlsa.snd_card_next(ref card);
                    if (returnCode < 0) break;
                }

                return list.ToArray();
            }
            catch
            {
                return Array.Empty<Card>();
            }
        }
    }
}
