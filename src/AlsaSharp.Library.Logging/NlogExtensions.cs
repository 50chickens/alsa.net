using System.Collections.Specialized;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Example.SNRReduction.Logging;

public static class NLogExtensions
{
    /// <summary>
    /// Configure Common.Logging to use the NLog factory adapter from AlsaSharp.Core
    /// so calls to Common.Logging.LogManager.GetLogger(...) are backed by NLog.
    /// </summary>
    public static ILoggingBuilder AddNlogFactoryAdaptor(this ILoggingBuilder builder)
    {
        // Set the Common.Logging adapter to the project's NLog adapter.
        Common.Logging.LogManager.Adapter = new AlsaSharp.Library.Logging.NLogLoggerFactoryAdapter(new NameValueCollection());
        return builder;
    }

    /// <summary>
    /// Apply a minimal programmatic NLog configuration (console target + Info+ rules).
    /// This moves programmatic logging setup out of `Program.cs` so it can be reused/tested.
    /// </summary>
    public static ILoggingBuilder AddNLogConfiguration(this ILoggingBuilder builder)
    {
        var nlogConfig = new LoggingConfiguration();
        var consoleTarget = new ColoredConsoleTarget("console")
        {
            // show only the class name (short logger name) instead of full namespace
            Layout = "${longdate}|${level:uppercase=true}|${logger:shortName=true}|${message}${onexception:${newline}${exception:format=tostring}}"
        };
        nlogConfig.AddTarget(consoleTarget);
        nlogConfig.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, consoleTarget);
        LogManager.Configuration = nlogConfig;

        return builder;
    }
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
