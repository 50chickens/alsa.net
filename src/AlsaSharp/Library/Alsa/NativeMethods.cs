using System.Runtime.InteropServices;
using AlsaSharp.Core.Native;

namespace AlsaSharp.Library.Alsa
{
    internal static class NativeMethods
    {
        const string AlsaLibrary = "libasound";
        const CallingConvention CConvention = CallingConvention.Cdecl;

        [DllImport(AlsaLibrary, CallingConvention = CConvention)]
        public static extern int snd_mixer_selem_set_enum_item(System.IntPtr elem, snd_mixer_selem_channel_id channel, uint idx);
    }
}
