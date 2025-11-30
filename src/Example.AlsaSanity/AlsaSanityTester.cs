using System.Runtime.InteropServices;

public class AlsaSanityTester
{
    public void TestSanity()
    {
        Console.WriteLine("Starting ALSA debug program...");
        int card = -1;
        int returnCode = Native.snd_card_next(ref card);
        Console.WriteLine($"snd_card_next returned {returnCode}, card={card}");
        while (card >= 0)
        {

            IntPtr p;
            returnCode = Native.snd_card_get_name(card, out p);
            Console.WriteLine($"snd_card_get_name returned rc={returnCode}, ptr={(p==IntPtr.Zero ? "<null>" : p.ToString())}");
            if (returnCode == 0 && p != IntPtr.Zero)
            {
                string name = Marshal.PtrToStringUTF8(p) ?? string.Empty;
                Console.WriteLine($"Card {card} name: '{name}'");
                Native.free(p);
            }

            IntPtr q;
            returnCode = Native.snd_card_get_longname(card, out q);
            Console.WriteLine($"snd_card_get_longname returned rc={returnCode}, ptr={(q==IntPtr.Zero ? "<null>" : q.ToString())}");
            if (returnCode == 0 && q != IntPtr.Zero)
            {
                string longname = Marshal.PtrToStringUTF8(q) ?? string.Empty;
                Console.WriteLine($"Card {card} longname: '{longname}'");
                Native.free(q);
            }
        }

        // Try opening the mixer for this card to exercise the same path

        Console.WriteLine($"Attempting mixer open/attach for card={card}");
        IntPtr mixer;
        int mrc = Native.snd_mixer_open(out mixer, 0);
        Console.WriteLine($"snd_mixer_open returned {mrc}, mixer={mixer}");
        string attachName = $"hw:{card}";
        int arc = Native.snd_mixer_attach(mixer, attachName);
        Console.WriteLine($"snd_mixer_attach('{attachName}') returned {arc}");
        int rrc = Native.snd_mixer_selem_register(mixer, IntPtr.Zero, IntPtr.Zero);
        Console.WriteLine($"snd_mixer_selem_register returned {rrc}");
        int lrc = Native.snd_mixer_load(mixer);
        Console.WriteLine($"snd_mixer_load returned {lrc}");
        IntPtr first = Native.snd_mixer_first_elem(mixer);
        Console.WriteLine($"snd_mixer_first_elem returned {first}");


        returnCode = Native.snd_card_next(ref card);
        Console.WriteLine($"snd_card_next returned {returnCode}, card={card}");
    }
}