using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Alsa.Net.Internal
{
    public static class AlsaCardEnumerator
    {
        // Returns an array of (cardIndex, name). If an error occurs, returns an empty array.
        public static (int Card, string Name)[] GetCards()
        {
            try
            {
                var list = new List<(int, string)>();
                int card = -1;
                int rc = InteropAlsa.snd_card_next(ref card);
                if (rc < 0) return Array.Empty<(int, string)>();

                while (card >= 0)
                {
                    string name = Marshal.PtrToStringUTF8(InteropAlsa.snd_card_get_name(card)) ?? string.Empty;
                    list.Add((card, name));
                    rc = InteropAlsa.snd_card_next(ref card);
                    if (rc < 0) break;
                }

                return list.ToArray();
            }
            catch
            {
                return Array.Empty<(int, string)>();
            }
        }
    }
}
