using AlsaSharp.Tests.Library;
using AlsaSharp.Tests.NUnit;
using Microsoft.Extensions.Configuration;

namespace AlsaSharp.Library.Logging.Tests
{
    [TestFixture]
    public class NUnitLoggerTests
    {
        private readonly IConfiguration _iconfiguration = TestUtils.BuildTestConfiguration();
        private ILog<NUnitLoggerTests> _log;
        public NUnitLoggerTests()
        {
            var logBuilder = new LogBuilder(_iconfiguration).UseNunitTestContext();
            logBuilder.Build();
            _log = LogManager.GetLogger<NUnitLoggerTests>();
            _log.Info($"Logger initialized for {GetType().Name}.");
        }
        [Test]
        public void Can_See_Log_Output_In_NUnit_Console()
        {
            _log.Info("This is a test log message from NUnitLoggerTests.");
            Assert.Pass("Test passed!");
        }
    }
}
