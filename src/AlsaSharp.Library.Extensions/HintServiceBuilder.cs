using AlsaSharp.Library.Logging;
using AlsaSharp.Library.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AlsaSharp.Library.Extensions
{
    public static class HintServiceBuilder
    {
        public static IHintService Build(IServiceProvider services)
        {
            var log= services.GetService<ILog<HintService>>();
            return new HintService(log);
        }
    }
}
