namespace AlsaSharp.Library.Native
{
    /// <summary>
    /// Public, minimal wrappers that expose safe diagnostics for a subset of libasound
    /// functions. These call into <see cref="InteropAlsa"/> via <see cref="NativeDiagnostics"/>
    /// and are intended for debug/diagnostic scenarios such as tests.
    /// </summary>
    public static class InteropDiagnostics
    {
        /// <summary>Calls <c>snd_card_next</c> to advance to the next ALSA card index.</summary>
        /// <param name="card">Reference to the current card index; updated to the next card on success.</param>
        /// <returns>Diagnostic result containing the return code from <c>snd_card_next</c>.</returns>
        public static DiagnosticResult<int> CardNext(ref int card)
        {
            try
            {
                var rc = InteropAlsa.snd_card_next(ref card);
                return new DiagnosticResult<int>(true, rc, null);
            }
            catch (Exception ex)
            {
                return new DiagnosticResult<int>(false, default!, ex.ToString());
            }
        }

        /// <summary>Calls <c>snd_card_get_name</c> to obtain the short name pointer for a card.</summary>
        /// <param name="card">The card index to query.</param>
        /// <returns>Diagnostic result with a tuple of (return code, native pointer).</returns>
        public static DiagnosticResult<(int rc, IntPtr ptr)> CardGetName(int card)
        {
            try
            {
                IntPtr p;
                var rc = InteropAlsa.snd_card_get_name(card, out p);
                return new DiagnosticResult<(int, IntPtr)>(true, (rc, p), null);
            }
            catch (Exception ex)
            {
                return new DiagnosticResult<(int, IntPtr)>(false, default, ex.ToString());
            }
        }

        /// <summary>Opens a mixer handle (<c>snd_mixer_open</c>).</summary>
        /// <param name="mixer">Outputs the opened mixer handle.</param>
        /// <returns>Diagnostic result containing the ALSA return code.</returns>
        public static DiagnosticResult<int> MixerOpen(out IntPtr mixer)
        {
            try
            {
                var rc = InteropAlsa.snd_mixer_open(out mixer, 0);
                return new DiagnosticResult<int>(true, rc, null);
            }
            catch (Exception ex)
            {
                mixer = IntPtr.Zero;
                return new DiagnosticResult<int>(false, default!, ex.ToString());
            }
        }

        /// <summary>Attaches a mixer to the given device name (<c>snd_mixer_attach</c>).</summary>
        /// <param name="mixer">Mixer handle.</param>
        /// <param name="name">Device name to attach (e.g. "hw:0").</param>
        /// <returns>Diagnostic result with the ALSA return code.</returns>
        public static DiagnosticResult<int> MixerAttach(IntPtr mixer, string name)
        {
            try
            {
                var rc = InteropAlsa.snd_mixer_attach(mixer, name);
                return new DiagnosticResult<int>(true, rc, null);
            }
            catch (Exception ex)
            {
                return new DiagnosticResult<int>(false, default!, ex.ToString());
            }
        }

        /// <summary>Registers simple element class for mixer operations (<c>snd_mixer_selem_register</c>).</summary>
        /// <param name="mixer">Mixer handle.</param>
        /// <returns>Diagnostic result with the ALSA return code.</returns>
        public static DiagnosticResult<int> MixerSelemRegister(IntPtr mixer)
        {
            try
            {
                var rc = InteropAlsa.snd_mixer_selem_register(mixer, IntPtr.Zero, IntPtr.Zero);
                return new DiagnosticResult<int>(true, rc, null);
            }
            catch (Exception ex)
            {
                return new DiagnosticResult<int>(false, default!, ex.ToString());
            }
        }

        /// <summary>Loads mixer elements from the attached device (<c>snd_mixer_load</c>).</summary>
        /// <param name="mixer">Mixer handle.</param>
        /// <returns>Diagnostic result with the ALSA return code.</returns>
        public static DiagnosticResult<int> MixerLoad(IntPtr mixer)
        {
            try
            {
                var rc = InteropAlsa.snd_mixer_load(mixer);
                return new DiagnosticResult<int>(true, rc, null);
            }
            catch (Exception ex)
            {
                return new DiagnosticResult<int>(false, default!, ex.ToString());
            }
        }

        /// <summary>Returns the first mixer element (<c>snd_mixer_first_elem</c>).</summary>
        /// <param name="mixer">Mixer handle.</param>
        /// <returns>Diagnostic result carrying the native element pointer.</returns>
        public static DiagnosticResult<IntPtr> MixerFirstElem(IntPtr mixer)
        {
            try
            {
                var p = InteropAlsa.snd_mixer_first_elem(mixer);
                return new DiagnosticResult<IntPtr>(true, p, null);
            }
            catch (Exception ex)
            {
                return new DiagnosticResult<IntPtr>(false, default!, ex.ToString());
            }
        }

        /// <summary>Frees a native pointer allocated by the library (<c>free</c>).</summary>
        /// <param name="ptr">Pointer to release.</param>
        public static void Free(IntPtr ptr) => InteropAlsa.free(ptr);

        /// <summary>Closes the mixer handle (<c>snd_mixer_close</c>).</summary>
        /// <param name="mixer">Mixer handle to close.</param>
        /// <returns>Diagnostic result with the ALSA return code.</returns>
        public static DiagnosticResult<int> MixerClose(IntPtr mixer)
        {
            try
            {
                var rc = InteropAlsa.snd_mixer_close(mixer);
                return new DiagnosticResult<int>(true, rc, null);
            }
            catch (Exception ex)
            {
                return new DiagnosticResult<int>(false, default!, ex.ToString());
            }
        }
    }
}
