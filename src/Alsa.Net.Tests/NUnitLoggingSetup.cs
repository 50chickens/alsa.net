using NLog;
using NLog.Config;
using NUnit.Framework;

namespace Alsa.Net.Tests
{
    [SetUpFixture]
    public class NUnitLoggingSetup
    {
        
        public void RegisterNUnitTarget()
        {
            var config = LogManager.Configuration ?? new LoggingConfiguration();
            // If the target is already registered, do not add again
            if (config.FindTargetByName("nunit_testcontext") == null)
            {
                var target = new NUnitTestContextTarget { Name = "nunit_testcontext", Layout = "${longdate} | ${logger:shortname=true} - ${level:uppercase=true} - ${message} ${exception:format=ToString}" };
                config.AddTarget(target.Name, target);
                var rule = new LoggingRule("*", LogLevel.Debug, target);
                config.LoggingRules.Add(rule);
                LogManager.Configuration = config;
            }
        }
    }
}
