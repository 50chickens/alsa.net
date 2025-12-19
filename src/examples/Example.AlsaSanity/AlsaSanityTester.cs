using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

/// <summary>
/// ALSA sanity tester for debugging.
/// </summary>
public class AlsaSanityTester(string label, ILogger<AlsaSanityTester> log)
{
    private readonly string _label = label ?? throw new ArgumentNullException("Label cannot be null");
    private readonly ILogger<AlsaSanityTester> _log = log ?? throw new ArgumentNullException(nameof(log));
    public void TestSanity()
    {
        _log.LogInformation("Starting ALSA debug program for {Label}...", _label);
        int card = -1;
        int returnCode = Native.snd_card_next(ref card);
        _log.LogInformation("snd_card_next -> rc={Rc}, card={Card}", returnCode, card);
        while (card >= 0)
        {
            IntPtr p;
            returnCode = Native.snd_card_get_name(card, out p);
            _log.LogInformation("snd_card_get_name -> rc={Rc}, ptr={Ptr}", returnCode, (p == IntPtr.Zero ? "<null>" : p.ToString()));
            if (returnCode == 0 && p != IntPtr.Zero)
            {
                string name = Marshal.PtrToStringUTF8(p) ;
                _log.LogInformation("Card {CardIndex} name: '{Name}'", card, name);
                Native.free(p);
            }

            IntPtr q;
            returnCode = Native.snd_card_get_longname(card, out q);
            _log.LogInformation("snd_card_get_longname -> rc={Rc}, ptr={Ptr}", returnCode, (q == IntPtr.Zero ? "<null>" : q.ToString()));
            if (returnCode == 0 && q != IntPtr.Zero)
            {
                string longname = Marshal.PtrToStringUTF8(q) ;
                _log.LogInformation("Card {CardIndex} longname: '{LongName}'", card, longname);
                Native.free(q);
            }
        }

        _log.LogInformation("Attempting mixer open/attach for card={Card}", card);
        IntPtr mixer;
        int mrc = Native.snd_mixer_open(out mixer, 0);
        _log.LogInformation("snd_mixer_open -> rc={Rc}, mixer={Mixer}", mrc, mixer);
        string attachName = $"hw:{card}";
        int arc = Native.snd_mixer_attach(mixer, attachName);
        _log.LogInformation("snd_mixer_attach({Attach}) -> rc={Rc}", attachName, arc);
        int rrc = Native.snd_mixer_selem_register(mixer, IntPtr.Zero, IntPtr.Zero);
        _log.LogInformation("snd_mixer_selem_register -> rc={Rc}", rrc);
        int lrc = Native.snd_mixer_load(mixer);
        _log.LogInformation("snd_mixer_load -> rc={Rc}", lrc);
        IntPtr first = Native.snd_mixer_first_elem(mixer);
        _log.LogInformation("snd_mixer_first_elem -> ptr={Ptr}", first);

        returnCode = Native.snd_card_next(ref card);
        _log.LogInformation("snd_card_next -> rc={Rc}, card={Card}", returnCode, card);
    }
}
