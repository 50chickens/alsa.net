using Alsa.Net.Core;
using Alsa.Net.Internal;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Alsa.Net.Tests
{
    [TestFixture]
    public class MixerProbeTests
    {
        private readonly IConfiguration _iconfiguration = BuildTestConfiguration();
        private ILog<MixerProbeTests> _log;

        [OneTimeSetUp]
        public void Setup()
        {
            var logBuilder = new LogBuilder(_iconfiguration).UseNunitTestContext();
            logBuilder.Build();
            _log = LogManager.GetLogger<MixerProbeTests>();
            _log.Info("Logger initialized for MixerProbeTests.");

        }
        [Test]
        [Category("Integration")]
        public void GetControlsForDefaultCard_ReturnsControls()
        {
            var alsaCardEnumerator = new AlsaCardEnumerator();
            var cards = alsaCardEnumerator.GetCards();
            Assert.IsNotNull(cards.FirstOrDefault(), "No ALSA cards found on system.");

        }
        public static IConfiguration BuildTestConfiguration()
        {
            {
                var values = new Dictionary<string, string?>
            {
                {"Logging:LogLevel", "Debug"},
                {"Logging:EnableConsoleLogging", "true"},
                {"Logging:EnableFileLogging", "false"},
            };
                var configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(values);
                return configurationBuilder.Build();
            }

        }
    }
}
