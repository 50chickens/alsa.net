using AlsaSharp.Internal.Audio;
using Microsoft.Extensions.DependencyInjection;

namespace AlsaSharp.Library.Extensions
{
    public static class HintServiceExtensions
    {
        public static IServiceCollection AddHintService(this IServiceCollection services, Func<IServiceProvider, IHintService> implementationFactory)
        {
            services.AddSingleton(implementationFactory);
            return services;
        }
    }
}