using NLog.Config;
using NLog.Targets;
using System;
namespace Alsa.Net.Core
{
    public static class Log
    {

    }
    public static class NunitBuilderExtensions
    {
        public static LogBuilder UseNunitTestContext(this LogBuilder logBuilder, Common.Logging.LogLevel? logLevel = null)
        {
            var target = new NunitLogTarget();
            var level = logLevel ?? logBuilder.Context.LogLevel;
            var loggingRule = new LoggingRule("*", level.ToNlogLogLevel(), target);
            logBuilder.AddRegistration(new LogRegistration
            {
                Target = target,
                LoggingRules = new List<LoggingRule> { loggingRule },
            });
            return logBuilder;
        }

    }

    public class NunitLogTarget : TargetWithLayout
    {

        public NunitLogTarget()
        {
            // use longdate, logger shortname, message, exception with innerexception
            Layout = "${longdate} | ${logger:shortname = true} - ${message} ${oneexception:inner=$newline}$exception:frmat=ToString}";
            Name = "Nunit";
        }
    }
    public static class NlogExtensions
    {
        public static NLog.LogLevel ToNlogLogLevel(this Common.Logging.LogLevel level)
        {
            return level switch
            {
                Common.Logging.LogLevel.All => NLog.LogLevel.Trace,
                Common.Logging.LogLevel.Trace => NLog.LogLevel.Trace,
                Common.Logging.LogLevel.Debug => NLog.LogLevel.Debug,
                Common.Logging.LogLevel.Info => NLog.LogLevel.Info,
                Common.Logging.LogLevel.Warn => NLog.LogLevel.Warn,
                Common.Logging.LogLevel.Error => NLog.LogLevel.Error,
                Common.Logging.LogLevel.Fatal => NLog.LogLevel.Fatal,
                Common.Logging.LogLevel.Off => NLog.LogLevel.Off,
                _ => NLog.LogLevel.Info,
            };
        }
    }
}
