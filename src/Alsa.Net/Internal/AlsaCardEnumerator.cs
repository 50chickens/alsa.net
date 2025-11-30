using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.IO;

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
                var names = ReadProcCardNames();
                int card = -1;
                int returnCode = InteropAlsa.snd_card_next(ref card);
                if (returnCode < 0) return Array.Empty<Card>();

                while (card >= 0)
                {
                    try
                    {
                        // Avoid calling snd_card_get_name (it may crash on some
                        // platforms). Prefer reading `/proc/asound/cards` which
                        // is available on Linux systems and provides the card id
                        // and short name. Fall back to empty name when parsing
                        // fails.
                        string name = names.TryGetValue(card, out var n) ? n : string.Empty;
                        list.Add(new Card(card, name));
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[AlsaCardEnumerator] exception reading name for card={card}: {ex}");
                        list.Add(new Card(card, string.Empty));
                    }

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

        private static Dictionary<int, string> ReadProcCardNames()
        {
            var dict = new Dictionary<int, string>();
            try
            {
                var path = "/proc/asound/cards";
                if (!File.Exists(path)) return dict;
                var lines = File.ReadAllLines(path);
                // Expect lines like: " 0 [IQaudIOCODEC     ]: ..."
                    var rx = new Regex("^\\s*(\\d+)\\s+\\[([^\\]]+)\\]", RegexOptions.Compiled);
                foreach (var line in lines)
                {
                    var m = rx.Match(line);
                    if (!m.Success) continue;
                    if (int.TryParse(m.Groups[1].Value, out var id))
                    {
                        var name = m.Groups[2].Value.Trim();
                        if (!string.IsNullOrEmpty(name)) dict[id] = name;
                    }
                }
            }
            catch
            {
                // best-effort only
            }

            return dict;
        }
    }
}
