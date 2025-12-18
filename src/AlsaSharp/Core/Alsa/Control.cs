using System.Runtime.InteropServices;
using AlsaSharp.Core.Native;
using AlsaSharp.Library.Logging;

namespace AlsaSharp.Core.Alsa;

/// <summary>
/// Default implementation of <see cref="IControl"/> that operates on a given card index.
/// </summary>
public class Control : IControl
{
    private int cardIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="Control"/> class for the given card.
    /// </summary>
    /// <param name="cardIndex">ALSA card index.</param>
    public Control(int cardIndex)
    {
        this.cardIndex = cardIndex;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetControlElementNames()
    {
        var results = new List<string>();
        IntPtr mixer = IntPtr.Zero;
        try
        {
            // open mixer for the card (e.g. "hw:0")
            int err = InteropAlsa.snd_mixer_open(out mixer, 0);
            if (err < 0 || mixer == IntPtr.Zero)
                return results;

            string name = $"hw:{cardIndex}";
            err = InteropAlsa.snd_mixer_attach(mixer, name);
            if (err < 0)
                return results;

            InteropAlsa.snd_mixer_selem_register(mixer, IntPtr.Zero, IntPtr.Zero);
            err = InteropAlsa.snd_mixer_load(mixer);
            if (err < 0)
                return results;

            IntPtr elem = InteropAlsa.snd_mixer_first_elem(mixer);
            while (elem != IntPtr.Zero)
            {
                IntPtr nptr = InteropAlsa.snd_mixer_selem_get_name(elem);
                string elemName = Marshal.PtrToStringUTF8(nptr) ?? string.Empty;
                if (!string.IsNullOrEmpty(elemName))
                    results.Add(elemName);
                elem = InteropAlsa.snd_mixer_elem_next(elem);
            }
        }
        catch (Exception ex)
        {
            LogManager.GetLogger<Control>()?.Error(ex, $"[ALSA ERROR] GetControlElementNames: {ex.Message}");
        }
        finally
        {
            try
            { if (mixer != IntPtr.Zero) InteropAlsa.snd_mixer_close(mixer); }
            catch (Exception ex) { LogManager.GetLogger<Control>()?.Error(ex, $"[ALSA ERROR] GetControlElementNames (closing mixer): {ex.Message}"); }
        }

        return results;
    }

    /// <inheritdoc />
    public int GetControlElementValue(string elementName)
    {
        IntPtr mixer = IntPtr.Zero;
        try
        {
            int err = InteropAlsa.snd_mixer_open(out mixer, 0);
            if (err < 0 || mixer == IntPtr.Zero)
                return 0;

            string name = $"hw:{cardIndex}";
            err = InteropAlsa.snd_mixer_attach(mixer, name);
            if (err < 0)
                return 0;

            InteropAlsa.snd_mixer_selem_register(mixer, IntPtr.Zero, IntPtr.Zero);
            err = InteropAlsa.snd_mixer_load(mixer);
            if (err < 0)
                return 0;

            IntPtr elem = FindElementByName(mixer, elementName);
            if (elem == IntPtr.Zero)
                return 0;

            unsafe
            {
                nint value = 0;
                // prefer mono reading for simplicity
                int r = InteropAlsa.snd_mixer_selem_get_playback_volume(elem, snd_mixer_selem_channel_id.SND_MIXER_SCHN_MONO, &value);
                if (r >= 0)
                    return (int)value;
                // try capture
                r = InteropAlsa.snd_mixer_selem_get_capture_volume(elem, snd_mixer_selem_channel_id.SND_MIXER_SCHN_MONO, &value);
                if (r >= 0)
                    return (int)value;
            }
        }
        catch (Exception ex)
        {
            LogManager.GetLogger<Control>()?.Error(ex, $"[ALSA ERROR] GetControlElementValue: {ex.Message}");
        }
        finally
        {
            try
            { if (mixer != IntPtr.Zero) InteropAlsa.snd_mixer_close(mixer); }
            catch (Exception ex) { LogManager.GetLogger<Control>()?.Error(ex, $"[ALSA ERROR] GetControlElementValue (closing mixer): {ex.Message}"); }
        }

        return 0;
    }

    /// <inheritdoc />
    public void SetControlElementValue(string elementName, int value)
    {
        IntPtr mixer = IntPtr.Zero;
        try
        {
            int err = InteropAlsa.snd_mixer_open(out mixer, 0);
            if (err < 0 || mixer == IntPtr.Zero)
                return;

            string name = $"hw:{cardIndex}";
            err = InteropAlsa.snd_mixer_attach(mixer, name);
            if (err < 0)
                return;

            InteropAlsa.snd_mixer_selem_register(mixer, IntPtr.Zero, IntPtr.Zero);
            err = InteropAlsa.snd_mixer_load(mixer);
            if (err < 0)
                return;

            IntPtr elem = FindElementByName(mixer, elementName);
            if (elem == IntPtr.Zero)
                return;

            // try to set playback value for all channels
            InteropAlsa.snd_mixer_selem_set_playback_volume_all(elem, (nint)value);
        }
        catch (Exception ex)
        {
            LogManager.GetLogger<Control>()?.Error(ex, $"[ALSA ERROR] SetControlElementValue: {ex.Message}");
        }
        finally
        {
            try
            { if (mixer != IntPtr.Zero) InteropAlsa.snd_mixer_close(mixer); }
            catch (Exception ex) { LogManager.GetLogger<Control>()?.Error(ex, $"[ALSA ERROR] SetControlElementValue (closing mixer): {ex.Message}"); }
        }
    }

    private IntPtr FindElementByName(IntPtr mixer, string elementName)
    {
        IntPtr elem = InteropAlsa.snd_mixer_first_elem(mixer);
        while (elem != IntPtr.Zero)
        {
            IntPtr nptr = InteropAlsa.snd_mixer_selem_get_name(elem);
            string name = Marshal.PtrToStringUTF8(nptr) ?? string.Empty;
            if (string.Equals(name, elementName, StringComparison.OrdinalIgnoreCase))
                return elem;
            elem = InteropAlsa.snd_mixer_elem_next(elem);
        }
        return IntPtr.Zero;
    }
}
