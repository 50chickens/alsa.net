namespace AlsaSharp.Library.Logging
{
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
