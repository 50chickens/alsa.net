using System.Runtime.InteropServices;
using AlsaSharp.Library.Logging;

/// <summary>
/// ALSA sanity tester for debugging.
/// </summary>
public class AlsaSanityTester(string label, ILog<AlsaSanityTester> log)
{
    private readonly string _label = label ?? throw new ArgumentNullException("Label cannot be null");
    private readonly ILog<AlsaSanityTester> _log = log ?? throw new ArgumentNullException(nameof(log));
    public void TestSanity()
    {
        _log.Info($"Starting ALSA debug program for {_label}...");
        int card = -1;
        int returnCode = Native.snd_card_next(ref card);
        _log.Info($"snd_card_next -> rc={returnCode}, card={card}");
        while (card >= 0)
        {
            IntPtr p;
            returnCode = Native.snd_card_get_name(card, out p);
            string cardNameFromPointer = p == IntPtr.Zero ? "<null>" : Marshal.PtrToStringUTF8(p) ?? "<invalid>";
            _log.Info($"snd_card_get_name -> rc={returnCode}, ptr={cardNameFromPointer}");
            if (returnCode == 0 && p != IntPtr.Zero)
            {
                string name = Marshal.PtrToStringUTF8(p) ;
                _log.Info($"Card {card} name: '{name}'");
                Native.free(p);
            }

            IntPtr q;
            returnCode = Native.snd_card_get_longname(card, out q);
            string cardLongNameFromPointer = q == IntPtr.Zero ? "<null>" : Marshal.PtrToStringUTF8(q) ?? "<invalid>";
            _log.Info($"snd_card_get_longname -> rc={returnCode}, ptr={cardLongNameFromPointer}");
            if (returnCode == 0 && q != IntPtr.Zero)
            {
                string longname = Marshal.PtrToStringUTF8(q) ;
                _log.Info($"Card {card} longname: '{longname}'");
                Native.free(q);
            }
        }

        _log.Info($"Attempting mixer open/attach for card={card}");
        IntPtr mixer;
        int mrc = Native.snd_mixer_open(out mixer, 0);
        _log.Info($"snd_mixer_open -> rc={mrc}, mixer={mixer}");
        string attachName = $"hw:{card}";
        int arc = Native.snd_mixer_attach(mixer, attachName);
        _log.Info($"snd_mixer_attach({attachName}) -> rc={arc}");
        int rrc = Native.snd_mixer_selem_register(mixer, IntPtr.Zero, IntPtr.Zero);
        _log.Info($"snd_mixer_selem_register -> rc={rrc}");
        int lrc = Native.snd_mixer_load(mixer);
        _log.Info($"snd_mixer_load -> rc={lrc}");
        IntPtr first = Native.snd_mixer_first_elem(mixer);
        _log.Info($"snd_mixer_first_elem -> ptr={first}");

        returnCode = Native.snd_card_next(ref card);
        _log.Info($"snd_card_next -> rc={returnCode}, card={card}");
    }
}
