using AlsaSharp.Library.Logging;
using AlsaSharp.Tests.NUnit;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace AlsaSharp.Tests
{
    [TestFixture]
    public class CardControlTests
    {
        private readonly IConfiguration _iconfiguration = TestUtils.BuildTestConfiguration();
        private ILog<CardControlTests> _log;

        [OneTimeSetUp]
        public void Setup()
        {
            var logBuilder = new LogBuilder(_iconfiguration).UseNunitTestContext();
            logBuilder.Build();
            _log = Library.Logging.LogManager.GetLogger<CardControlTests>();
            _log.Info($"Logger initialized for {GetType().Name}.");

        }
        [Test]
        [Category("Integration")]
        public void GetControlsForDefaultCard_ReturnsControls()
        {
            _log.Info("Starting GetControlsForDefaultCard_ReturnsControls test.");
            
        }
        
    }
}
