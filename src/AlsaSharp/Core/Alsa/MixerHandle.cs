using AlsaSharp.Core.Native;
using System;

namespace AlsaSharp.Core.Alsa
{
    /// <summary>
    /// Lightweight wrapper around a libasound mixer handle for a given card.
    /// Opens and loads the mixer on construction and closes on dispose.
    /// </summary>
    public sealed class MixerHandle : IDisposable
    {
        /// <summary>Underlying native mixer handle.</summary>
        public IntPtr Handle { get; private set; }

        /// <summary>Indicates whether the mixer handle is open.</summary>
        public bool IsOpen => Handle != IntPtr.Zero;

        /// <summary>
        /// Opens and initializes a mixer handle for the given card index.
        /// </summary>
        /// <param name="card">ALSA card index.</param>
        public MixerHandle(int card)
        {
            if (InteropAlsa.snd_mixer_open(out var h, 0) < 0) { Handle = IntPtr.Zero; return; }
            Handle = h;

            var attachName = $"hw:{card}";
            if (InteropAlsa.snd_mixer_attach(Handle, attachName) < 0) { Close(); return; }
            if (InteropAlsa.snd_mixer_selem_register(Handle, IntPtr.Zero, IntPtr.Zero) < 0) { Close(); return; }
            if (InteropAlsa.snd_mixer_load(Handle) < 0) { Close(); return; }
        }

        /// <summary>Returns the first mixer element pointer or <see cref="IntPtr.Zero"/>.</summary>
        public IntPtr FirstElem() => IsOpen ? InteropAlsa.snd_mixer_first_elem(Handle) : IntPtr.Zero;

        /// <summary>Returns the next mixer element pointer following <paramref name="elem"/>.</summary>
        public IntPtr NextElem(IntPtr elem) => InteropAlsa.snd_mixer_elem_next(elem);

        /// <summary>Finds an element by its name and returns the native pointer or zero if not found.</summary>
        /// <param name="name">Element name to find.</param>
        /// <returns>Pointer to the element or <see cref="IntPtr.Zero"/>.</returns>
        public IntPtr FindElementByName(string name)
        {
            if (!IsOpen) return IntPtr.Zero;
            var elem = FirstElem();
            while (elem != IntPtr.Zero)
            {
                var ptr = InteropAlsa.snd_mixer_selem_get_name(elem);
                if (ptr == IntPtr.Zero) { elem = NextElem(elem); continue; }
                var nm = System.Runtime.InteropServices.Marshal.PtrToStringUTF8(ptr);
                if (nm == null) { elem = NextElem(elem); continue; }
                if (string.Equals(nm, name, StringComparison.Ordinal)) return elem;
                elem = NextElem(elem);
            }
            return IntPtr.Zero;
        }

        /// <summary>Closes the mixer handle and frees native resources.</summary>
        public void Close()
        {
            if (Handle != IntPtr.Zero)
            {
                InteropAlsa.snd_mixer_close(Handle);
                Handle = IntPtr.Zero;
            }
        }

        /// <summary>Releases the mixer handle.</summary>
        public void Dispose() => Close();
    }
}
