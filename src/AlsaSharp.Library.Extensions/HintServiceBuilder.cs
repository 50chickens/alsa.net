using AlsaSharp.Internal.Audio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AlsaSharp.Library.Extensions
{
    public static class HintServiceBuilder
    {
        public static IHintService Build(IServiceProvider services)
        {
            var logger = services.GetService<ILogger<HintService>>();
            return new HintService(logger);
        }
    }
}