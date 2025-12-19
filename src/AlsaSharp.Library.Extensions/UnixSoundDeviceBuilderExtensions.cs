using AlsaSharp.Library.Builders;
using AlsaSharp.Library.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AlsaSharp.Library.Extensions;

public static class UnixSoundDeviceBuilderExtensions
{
    public static IServiceCollection AddUnixSoundDeviceBuilder(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Ensure the hint service is registered
        services.AddHintService(HintServiceBuilder.Build);
        
        var serviceCollection = services.AddSingleton(UnixSoundDeviceBuilder.Build);
        return services;
    }
}
