using System.Runtime.InteropServices;

namespace AlsaSharp.Internal;

/// <summary>
/// Provides P/Invoke signatures for libasound (ALSA) functions used by this library.
/// </summary>
internal static class InteropAlsa
{
    const string AlsaLibrary = "libasound";

    const CallingConvention CConvention = CallingConvention.Cdecl;
    const CharSet CSet = CharSet.Ansi;

    /// <summary>
    /// Gets a human-readable string describing the error code.
    /// </summary>
    /// <param name="errnum">The error code.</param>
    /// <returns>A human-readable string describing the error code.</returns>
    [DllImport(AlsaLibrary, CallingConvention = CConvention, CharSet = CSet)]
    public static extern IntPtr snd_strerror(int errnum); // returns const char*

    [DllImport(AlsaLibrary, CallingConvention = CConvention, CharSet = CSet)]
    public static extern int snd_pcm_open(ref IntPtr pcm, string name, snd_pcm_stream_t stream, int mode);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_card_next(ref int card);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_card_get_name(int card, out IntPtr name);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_card_get_longname(int card, out IntPtr name);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_pcm_start(IntPtr pcm);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_pcm_pause(IntPtr pcm, int enable);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_pcm_resume(IntPtr pcm);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_pcm_drain(IntPtr pcm);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_pcm_drop(IntPtr pcm);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_pcm_close(IntPtr pcm);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_pcm_recover(IntPtr pcm, int err, int silent);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern nint snd_pcm_writei(IntPtr pcm, IntPtr buffer, nuint size);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern nint snd_pcm_readi(IntPtr pcm, IntPtr buffer, nuint size);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_pcm_set_params(IntPtr pcm, snd_pcm_format_t format, snd_pcm_access_t access, uint channels, uint rate, int softResample, uint latency);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_pcm_hw_params_malloc(ref IntPtr @params);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_pcm_hw_params_any(IntPtr pcm, IntPtr @params);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_pcm_hw_params_set_access(IntPtr pcm, IntPtr @params, snd_pcm_access_t access);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_pcm_hw_params_set_format(IntPtr pcm, IntPtr @params, snd_pcm_format_t val);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_pcm_hw_params_set_channels(IntPtr pcm, IntPtr @params, uint val);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern unsafe int snd_pcm_hw_params_set_rate_near(IntPtr pcm, IntPtr @params, uint* val, int* dir);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_pcm_hw_params(IntPtr pcm, IntPtr @params);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern unsafe int snd_pcm_hw_params_get_period_size(IntPtr @params, nuint* frames, int* dir);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern unsafe int snd_pcm_hw_params_set_period_size_near(IntPtr pcm, IntPtr @params, nuint* frames, int* dir);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_mixer_open(out IntPtr mixer, int mode);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_mixer_close(IntPtr mixer);

    [DllImport(AlsaLibrary, CallingConvention = CConvention, CharSet = CSet)]
    public static extern int snd_mixer_attach(IntPtr mixer, string name);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_mixer_load(IntPtr mixer);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_mixer_selem_register(IntPtr mixer, IntPtr options, IntPtr classp);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern IntPtr snd_mixer_first_elem(IntPtr mixer);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern IntPtr snd_mixer_elem_next(IntPtr elem);

    /// <summary>
    /// Gets the name of the given mixer simple element.
    /// </summary>
    /// <param name="elem"></param>
    /// <returns></returns>
    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern IntPtr snd_mixer_selem_get_name(IntPtr elem);

    // Free memory allocated by ALSA helper functions that return allocated strings
    [DllImport("libc", CallingConvention = CConvention)]
    public static extern void free(IntPtr ptr);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_mixer_selem_id_malloc(ref IntPtr selemId);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern void snd_mixer_selem_id_free(IntPtr selemId);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern unsafe int snd_mixer_selem_get_playback_volume(IntPtr elem, snd_mixer_selem_channel_id channel, nint* value);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_mixer_selem_set_playback_volume(IntPtr elem, snd_mixer_selem_channel_id channel, nint value);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_mixer_selem_set_playback_volume_all(IntPtr elem, nint value);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_mixer_selem_set_playback_switch_all(IntPtr elem, int value);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_mixer_selem_set_playback_switch(IntPtr elem, snd_mixer_selem_channel_id channel, int value);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern unsafe int snd_mixer_selem_get_playback_volume_range(IntPtr elem, nint* min, nint* max);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_mixer_selem_set_playback_volume_range(IntPtr elem, nint min, nint max);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern unsafe int snd_mixer_selem_get_capture_volume(IntPtr elem, snd_mixer_selem_channel_id channel, nint* value);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_mixer_selem_set_capture_volume(IntPtr elem, snd_mixer_selem_channel_id channel, nint value);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_mixer_selem_set_capture_volume_all(IntPtr elem, nint value);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_mixer_selem_set_capture_switch_all(IntPtr elem, int value);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_mixer_selem_set_capture_switch(IntPtr elem, snd_mixer_selem_channel_id channel, int value);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern unsafe int snd_mixer_selem_get_capture_volume_range(IntPtr elem, nint* min, nint* max);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_mixer_selem_set_capture_volume_range(IntPtr elem, nint min, nint max);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_mixer_selem_has_playback_channel(IntPtr elem, snd_mixer_selem_channel_id channel);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_mixer_selem_has_playback_volume(IntPtr elem);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_mixer_selem_has_capture_channel(IntPtr elem, snd_mixer_selem_channel_id channel);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern unsafe int snd_mixer_selem_get_playback_switch(IntPtr elem, snd_mixer_selem_channel_id channel, int* value);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern unsafe int snd_mixer_selem_get_capture_switch(IntPtr elem, snd_mixer_selem_channel_id channel, int* value);

    [DllImport(AlsaLibrary, CallingConvention = CConvention, EntryPoint = "snd_mixer_selem_get_playback_dB")]
    public static extern unsafe int snd_mixer_selem_get_playback_dB(IntPtr elem, snd_mixer_selem_channel_id channel, long* value);

    [DllImport(AlsaLibrary, CallingConvention = CConvention, EntryPoint = "snd_mixer_selem_get_capture_dB")]
    public static extern unsafe int snd_mixer_selem_get_capture_dB(IntPtr elem, snd_mixer_selem_channel_id channel, long* value);

    public static string StrError(int errno) => Marshal.PtrToStringUTF8(snd_strerror(errno)) ?? $"errno {errno}";

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_device_name_hint(int card, string iface, out IntPtr hints);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern void snd_device_name_free_hint(IntPtr hints);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern IntPtr snd_device_name_get_hint(IntPtr hint, string key);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_ctl_open(out IntPtr ctl, string name, int mode);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_ctl_close(IntPtr ctl);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_ctl_card_info_malloc(out IntPtr info);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern void snd_ctl_card_info_free(IntPtr info);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_ctl_card_info(IntPtr ctl, IntPtr info);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern IntPtr snd_ctl_card_info_get_id(IntPtr info);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern IntPtr snd_ctl_card_info_get_driver(IntPtr info);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern IntPtr snd_ctl_card_info_get_name(IntPtr info);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern IntPtr snd_ctl_card_info_get_longname(IntPtr info);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern IntPtr snd_ctl_card_info_get_mixername(IntPtr info);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern IntPtr snd_ctl_card_info_get_components(IntPtr info);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_ctl_pcm_next_device(IntPtr ctl, ref int device);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_ctl_pcm_info(IntPtr ctl, IntPtr pcminfo);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_pcm_info_malloc(out IntPtr info);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern void snd_pcm_info_free(IntPtr info);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_pcm_info_set_device(IntPtr pcmInfo, int device);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_pcm_info_set_subdevice(IntPtr pcmInfo, int subdevice);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_pcm_info_set_stream(IntPtr pcmInfo, snd_pcm_stream_t stream);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern IntPtr snd_pcm_info_get_id(IntPtr pcmInfo);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern IntPtr snd_pcm_info_get_name(IntPtr pcmInfo);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern IntPtr snd_pcm_info_get_subdevice_name(IntPtr pcmInfo);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_pcm_info_get_subdevices_count(IntPtr pcmInfo);

    // Control element list helpers (used to compute controls_count similar to alsactl)
    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_ctl_elem_list_malloc(out IntPtr list);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_ctl_elem_list(IntPtr ctl, IntPtr list);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern int snd_ctl_elem_list_get_count(IntPtr list);

    [DllImport(AlsaLibrary, CallingConvention = CConvention)]
    public static extern void snd_ctl_elem_list_free(IntPtr list);

}