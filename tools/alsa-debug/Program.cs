using System;
using System.Runtime.InteropServices;

internal static class Native
{
    const string Lib = "libasound";
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int snd_card_next(ref int card);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int snd_card_get_name(int card, out IntPtr name);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int snd_card_get_longname(int card, out IntPtr name);

    [DllImport("libc", CallingConvention = CallingConvention.Cdecl)]
    public static extern void free(IntPtr ptr);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int snd_mixer_open(out IntPtr mixer, int mode);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int snd_mixer_attach(IntPtr mixer, string name);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int snd_mixer_selem_register(IntPtr mixer, IntPtr options, IntPtr classp);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int snd_mixer_load(IntPtr mixer);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr snd_mixer_first_elem(IntPtr mixer);
}

class Program
{
    static void Main()
    {
        Console.WriteLine("Starting ALSA debug program...");
        int card = -1;
        int rc = Native.snd_card_next(ref card);
        Console.WriteLine($"snd_card_next returned {rc}, card={card}");
        while (card >= 0)
        {
            try
            {
                IntPtr p;
                int rc = Native.snd_card_get_name(card, out p);
                Console.WriteLine($"snd_card_get_name returned rc={rc}, ptr={(p==IntPtr.Zero?"<null>":p.ToString())}");
                if (rc == 0 && p != IntPtr.Zero)
                {
                    string name = Marshal.PtrToStringUTF8(p) ?? string.Empty;
                    Console.WriteLine($"Card {card} name: '{name}'");
                    Native.free(p);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"snd_card_get_name threw: {ex}");
            }

            try
            {
                IntPtr q;
                int rc2 = Native.snd_card_get_longname(card, out q);
                Console.WriteLine($"snd_card_get_longname returned rc={rc2}, ptr={(q==IntPtr.Zero?"<null>":q.ToString())}");
                if (rc2 == 0 && q != IntPtr.Zero)
                {
                    string longname = Marshal.PtrToStringUTF8(q) ?? string.Empty;
                    Console.WriteLine($"Card {card} longname: '{longname}'");
                    Native.free(q);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"snd_card_get_longname threw: {ex}");
            }

            // Try opening the mixer for this card to exercise the same path
            try
            {
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Mixer sequence threw: {ex}");
            }

            rc = Native.snd_card_next(ref card);
            Console.WriteLine($"snd_card_next returned {rc}, card={card}");
        }

        Console.WriteLine("Done.");
    }
}