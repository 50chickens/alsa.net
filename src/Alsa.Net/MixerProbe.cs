// MixerProbe facade: small public API delegating to internal components.
using System;
using Alsa.Net.Internal;

namespace Alsa.Net
{
    /// <summary>
    /// Public facade for probing mixer controls and setting values.
    /// Delegates the heavy work to internal classes (finder/enumerator/setter).
    /// </summary>
    public class MixerProbe
    {
        readonly MixerEnumerator _enumerator = new MixerEnumerator();
        readonly MixerSetter _setter = new MixerSetter();

        public MixerControlInfo[] GetControlsForCard(int card) => _enumerator.GetControlsForCard(card);

        public bool TrySetPlaybackVolume(int card, string controlName, string channelName, nint value)
            => _setter.TrySetPlaybackVolume(card, controlName, channelName, value);

        public bool TrySetCaptureVolume(int card, string controlName, string channelName, nint value)
            => _setter.TrySetCaptureVolume(card, controlName, channelName, value);

        public bool TrySetPlaybackSwitch(int card, string controlName, string channelName, int state)
            => _setter.TrySetPlaybackSwitch(card, controlName, channelName, state);

        public bool TrySetCaptureSwitch(int card, string controlName, string channelName, int state)
            => _setter.TrySetCaptureSwitch(card, controlName, channelName, state);
    }
}
