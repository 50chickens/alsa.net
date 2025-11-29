using Microsoft.Extensions.Configuration;
using NLog.Config;

namespace Alsa.Net.Core
{
    
    public class LogBuilder
    {

        private List<LogRegistration> _logRegistrations = new List<LogRegistration>();
        public readonly LogBuilderContext Context;

        public LogBuilder(IConfiguration configuration)
        {
            Context = new LogBuilderContext(configuration);
        }
        public void Build()
        {
            //initialize Nlog adaptor for Common.Logging. this configures common logging to use Nlog as the backend
            Common.Logging.LogManager.Adapter = new NLogLoggerFactoryAdapter(new System.Collections.Specialized.NameValueCollection());
            var nlogconfig = new LoggingConfiguration();
            //register each LogRegistration With NLog
            foreach (var logRegistration in _logRegistrations)
            {
                nlogconfig.AddTarget("target", logRegistration.Target);
            }
        }
        public void AddRegistration(LogRegistration registration)
        {
            _logRegistrations.Add(registration);
        }
    }
}
