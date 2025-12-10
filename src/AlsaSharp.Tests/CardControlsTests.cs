using NUnit.Framework;
using AlsaSharp.Internal;
using AlsaSharp.Library.Logging;
using System.Linq;
using AlsaSharp; // for ISoundDevice and UnixSoundDeviceBuilder

namespace AlsaSharp.Tests
{
    [TestFixture]
    public class CardControlsTests
    {
        [Test]
        [Category("Integration")]
        public void GetControls_ForFirstCard_ReturnsArray()
        {
            // Enumerate sound devices exposed by libasound
            var devices = UnixSoundDeviceBuilder.Build();
            var first = devices.FirstOrDefault();
            Assert.IsNotNull(first, "No ALSA sound devices found on system to test controls.");

            // Create a manager and retrieve simple mixer elements for the discovered device
            var log = LogManager.GetLogger<ISoundDeviceManager>();
            var mixerName = first.Settings.MixerDeviceName;
            Assert.IsNotNull(mixerName, "Mixer device name is null or not available.");
            var soundDeviceManager = new SoundDeviceManager(log, 0, mixerName);
            var controls = soundDeviceManager.GetMixerSimpleElements(first);
            Assert.IsNotNull(controls, "GetMixerControls returned null");

            TestContext.Progress.WriteLine($"Found {controls.Count} controls for device '{first.Settings.MixerDeviceName}'");

            // Dispose any devices created by the builder
            foreach (var d in devices)
            {
                try { d.Dispose(); } catch { }
            }
        }
    }
}
