using Common.Logging;
using Common.Logging.Factory;
using System.Collections.Specialized;

namespace Alsa.Net.Core
{
    public class NLogLoggerFactoryAdapter : AbstractCachingLoggerFactoryAdapter
    {
        public NLogLoggerFactoryAdapter(NameValueCollection properties)
        {
        }

        protected override ILog CreateLogger(string name)
        {
            return new NLogLogger(NLog.LogManager.GetLogger(name));
        }
    }
}
