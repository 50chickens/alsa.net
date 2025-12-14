using AlsaSharp.Library.Builders;
using AlsaSharp.Library.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AlsaSharp.Library.Extensions;

public static class UnixSoundDeviceBuilderExtensions
{
    public static IServiceCollection AddUnixSoundDeviceBuilder(this IServiceCollection services, string? measurementFolder = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        // Ensure the hint service is registered
        services.AddHintService(HintServiceBuilder.Build);

        // Register ISoundDevice instances built from discovered cards; pass measurementFolder to builder
        services.AddSingleton<IEnumerable<AlsaSharp.ISoundDevice>>(sp => UnixSoundDeviceBuilder.Build(sp, measurementFolder));

        return services;
    }
}
