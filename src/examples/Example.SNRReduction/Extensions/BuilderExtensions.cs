using System;
using Example.SNRReduction.Audio;
using Example.SNRReduction.Services;
using Examples.SNRReduction.Models;

namespace Example.SNRReduction.Extensions;


static class AddAudioServiceExtension
{
    public static IServiceCollection AddAudioService(this IServiceCollection services, Action<SNRReductionServiceOptions> configureOptions)
    {
        services.Configure(configureOptions);
        //inject  AudioInterfaceLevelMeter with the options from SNRReductionOptions

        services.AddSingleton<IAudioInterfaceLevelMeter, AudioInterfaceLevelMeter>();
        return services;
    }
}
