using System;

namespace AlsaSharp.Internal
{
    /// <summary>
    /// Lightweight wrapper around a libasound mixer handle for a given card.
    /// Opens and loads the mixer on construction and closes on dispose.
    /// </summary>
    public sealed class MixerHandle : IDisposable
    {
        public IntPtr Handle { get; private set; }

        public bool IsOpen => Handle != IntPtr.Zero;

        public MixerHandle(int card)
        {
            if (InteropAlsa.snd_mixer_open(out var h, 0) < 0) { Handle = IntPtr.Zero; return; }
            Handle = h;

            var attachName = $"hw:{card}";
            if (InteropAlsa.snd_mixer_attach(Handle, attachName) < 0) { Close(); return; }
            if (InteropAlsa.snd_mixer_selem_register(Handle, IntPtr.Zero, IntPtr.Zero) < 0) { Close(); return; }
            if (InteropAlsa.snd_mixer_load(Handle) < 0) { Close(); return; }
        }

        public IntPtr FirstElem() => IsOpen ? InteropAlsa.snd_mixer_first_elem(Handle) : IntPtr.Zero;

        public IntPtr NextElem(IntPtr elem) => InteropAlsa.snd_mixer_elem_next(elem);

        public IntPtr FindElementByName(string name)
        {
            if (!IsOpen) return IntPtr.Zero;
            var elem = FirstElem();
            while (elem != IntPtr.Zero)
            {
                var ptr = InteropAlsa.snd_mixer_selem_get_name(elem);
                var nm = ptr != IntPtr.Zero ? System.Runtime.InteropServices.Marshal.PtrToStringUTF8(ptr) ?? string.Empty : string.Empty;
                if (string.Equals(nm, name, StringComparison.Ordinal)) return elem;
                elem = NextElem(elem);
            }
            return IntPtr.Zero;
        }

        public void Close()
        {
            if (Handle != IntPtr.Zero)
            {
                InteropAlsa.snd_mixer_close(Handle);
                Handle = IntPtr.Zero;
            }
        }

        public void Dispose() => Close();
    }
}
