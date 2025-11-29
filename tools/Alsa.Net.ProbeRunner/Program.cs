using System;
using System.Text.Json;

namespace Alsa.Net.ProbeRunner;

internal static class Program
{
    private static int Main(string[] args)
    {
        int card = 0;
        if (args.Length > 0 && int.TryParse(args[0], out var v)) card = v;

        try
        {
            Console.WriteLine($"ProbeRunner: starting for card {card}");
            var cards = Alsa.Net.Internal.AlsaCardEnumerator.GetCards();
            Console.WriteLine($"ProbeRunner: cards={cards.Length}");

            var controls = Alsa.Net.MixerProbe.GetControlsForCard(card);
            var summary = new { Card = card, Count = controls?.Length ?? 0 };
            Console.WriteLine(JsonSerializer.Serialize(summary));
            return (controls != null && controls.Length > 0) ? 0 : 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 3;
        }
    }
}
