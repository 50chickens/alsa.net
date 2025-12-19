using AlsaSharp.Library.Logging;
using AlsaSharp.Library.Services;
using AlsaSharp.Tests.Library;
using AlsaSharp.Tests.NUnit;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

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

    }
}
