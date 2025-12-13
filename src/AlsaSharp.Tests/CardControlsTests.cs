using NUnit.Framework;
using AlsaSharp.Library.Logging;
using AlsaSharp.Tests.Library;
using AlsaSharp.Tests.NUnit;
using Microsoft.Extensions.Configuration;
using System.Linq;
using AlsaSharp.Library.Builders;
using AlsaSharp.Library.Services;

namespace AlsaSharp.Tests
{
    [TestFixture]
    public class CardControlsTests
    {
        private readonly IConfiguration _iconfiguration = TestUtils.BuildTestConfiguration();
        private ILog<CardControlsTests> _log;

        private ILog<ISoundDeviceManager> _soundDeviceManagerLog;

        [OneTimeSetUp]
        public void Setup()
        {
            var logBuilder = new LogBuilder(_iconfiguration).UseNunitTestContext();
            logBuilder.Build();
            _log = LogManager.GetLogger<CardControlsTests>();
            _soundDeviceManagerLog = LogManager.GetLogger<ISoundDeviceManager>();
            _log.Info($"Logger initialized for {GetType().Name}.");
        }

        [Test]
        [Category("Integration")]
        public void GetControls_ForFirstCard_ReturnsArray()
        {
            // Enumerate sound devices exposed by libasound
            var devices = UnixSoundDeviceBuilder.Build();
            var first = devices.FirstOrDefault();
            Assert.IsNotNull(first, "No ALSA sound devices found on system to test controls.");

            // Create a manager and retrieve simple mixer elements for the discovered device
            var mixerName = first.Settings.MixerDeviceName;
            Assert.IsNotNull(mixerName, "Mixer device name is null or not available.");
            var soundDeviceManager = new SoundDeviceManager(_soundDeviceManagerLog, 0, mixerName);
            var controls = soundDeviceManager.GetMixerSimpleElements(first);
            _log.Info($"Found {controls.Count} controls for device '{first.Settings.MixerDeviceName}'");
            Assert.IsNotNull(controls, "GetMixerControls returned null");
        }
    }
}
