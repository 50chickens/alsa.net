using AlsaSharp.Library.Logging;
using AlsaSharp.Tests.NUnit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NUnit.Framework;

namespace AlsaSharp.Tests
{
    [TestFixture]
    public class SoundDeviceSettingsTests
    {
        private readonly IConfiguration _iconfiguration = TestUtils.BuildTestConfiguration();
        private ILog<NUnitLoggerTests> _log;

        [OneTimeSetUp]
        public void Setup()
        {
            var logBuilder = new LogBuilder(_iconfiguration).UseNunitTestContext();
            logBuilder.Build();
            _log = LogManager.GetLogger<NUnitLoggerTests>();
            _log.Info($"Logger initialized for {GetType().Name}.");
        }
        [Test]
        public void Can_Create_AlsaDevice()
        {
            // create a config for your audio hardware setup
            var config = new SoundDeviceSettings
            {
                RecordingDeviceName = "default", // alsa name of recording device. use arecord -L for a list of available devices. "default" for systems default
                PlaybackDeviceName = "default", // alsa name of playback device. use aplay -L for a list of available devices. "default" for systems default
                MixerDeviceName = "default", // alsa name of mixer device. ensure your device actually supports this. some might not have volume channels etc.

                RecordingBitsPerSample = 16, // bit depth of recorded audio data. check your devices capabilities on values to set here. default is 16
                RecordingChannels = 2, // number of audio channels to record. default is 2
                RecordingSampleRate = 8000 // number of samples per second of the recorded audio data. check your device on values to set here. default is 8000 even though this sounds terrible
            };

            // create virtual interface to use your config
            using var alsaDevice = AlsaDeviceBuilder.Create(config);
             var message = JsonConvert.SerializeObject(new { alsaDevice });
             _log.Info(message);
        }
    }
}
