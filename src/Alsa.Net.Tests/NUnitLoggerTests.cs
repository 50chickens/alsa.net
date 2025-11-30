using Alsa.Net.Core;
using Microsoft.Extensions.Configuration;
using NLog.Config;
using NUnit.Framework;

namespace Alsa.Net.Tests
{
    [TestFixture]
    public class NUnitLoggerTests
    {
        private readonly IConfiguration _iconfiguration = TestUtils.BuildTestConfiguration();
        private ILog<NUnitLoggerTests> _log;

        [OneTimeSetUp]
        public void Setup()
        {
            // Register the custom NUnitTestContextTarget for this test run
            var config = NLog.LogManager.Configuration ?? new LoggingConfiguration();
            var nUnitTarget = new NUnitLogTarget();
            config.AddTarget("nunit", nUnitTarget);
            config.AddRuleForAllLevels(nUnitTarget);
            NLog.LogManager.Configuration = config;

            var logBuilder = new LogBuilder(_iconfiguration);
            logBuilder.Build();
            _log = LogManager.GetLogger<NUnitLoggerTests>();
            _log.Info("Logger initialized for NUnitLoggerTests.");
        }
        [Test]
        public void Can_See_Log_Output_In_NUnit_Console()
        {
            _log.Info("This is a test log message from NUnitLoggerTests.");
            Assert.Pass("Test passed!");
        }
    }
}
