using System;
using Example.SNRReduction.Audio;
using Example.SNRReduction.Services;
using Example.SNRReduction.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Example.SNRReduction.Extensions;


static class AddAudioServiceExtension
{
    public static IServiceCollection AddAudioService(this IServiceCollection services, Action<SNRReductionServiceOptions> configureOptions)
    {
        services.Configure(configureOptions);
        // audio service registrations are performed in Program.cs so the audio device
        // factory can be created using runtime configuration (AudioCardOptions)
        return services;
    }
}
