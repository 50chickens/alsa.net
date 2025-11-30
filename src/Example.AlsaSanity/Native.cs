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
