using AlsaSharp.Library.Builders;
using AlsaSharp.Library.Extensions;
using AlsaSharp.Library.Logging;
using AlsaSharp.Library.Services;
using Example.SNRReduction.Audio;
using Example.SNRReduction.Extensions;
using Example.SNRReduction.Logging;
using Example.SNRReduction.Models;
using Example.SNRReduction.Services;
using Microsoft.Extensions.Options;
using NLog.Extensions.Logging;

namespace Example.SNRReduction;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Configuration.AddEnvironmentVariables(prefix: "SNR_");

        var switchMappings = new Dictionary<string, string>
        {
            { "--AutoSweep", "SNRReduction:AutoSweep" },
                { "--AutoConfigureDaiMux", "SNRReduction:AutoConfigureDaiMux" },
            { "--AudioCardName", "SNRReduction:AudioCardName" },
            { "--ApplyAlsaStateFile", "SNRReduction:ApplyAlsaStateFile" },

        };

        builder.Configuration.AddCommandLine(args, switchMappings);
        builder.Services.AddOptions<SNRReductionServiceOptions>().Bind(builder.Configuration.GetSection(SNRReductionServiceOptions.Settings));
        builder.Services.AddOptions<AudioLevelMeterRecorderServiceOptions>().Bind(builder.Configuration.GetSection(AudioLevelMeterRecorderServiceOptions.Settings));
        builder.Services.AddOptions<AudioCardOptions>().Bind(builder.Configuration.GetSection(AudioCardOptions.Settings));
        builder.Services.AddSingleton(new ControlSweepOptions(new List<AlsaControl>()));

        // Register logger first so it's available for all other services
        builder.Services.AddSingleton(typeof(ILog<>), typeof(NLogAdapter<>));
        
        // Register core services
        builder.Services.AddSingleton<FileNameGenerator>();
        builder.Services.AddSingleton<AudioCardProberService>();
        builder.Services.AddSingleton<HintService>();
        builder.Services.AddSingleton<IHintService>(sp => sp.GetRequiredService<HintService>());
        builder.Services.AddSingleton<IAudioDeviceBuilder, AudioDeviceBuilder>();
        builder.Services.AddSingleton<IAudioInterfaceLevelMeterService, AudioInterfaceLevelMeter>();
        builder.Services.AddSingleton<IAudioLevelMeterRecorderService, AudioLevelMeterRecorderService>();

        var snrSection = builder.Configuration.GetSection(SNRReductionServiceOptions.Settings);
        
        builder.Services.Configure<AudioCardOptions>(builder.Configuration.GetSection(AudioCardOptions.Settings));
        builder.Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<SNRReductionServiceOptions>>().Value);
        builder.Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<AudioLevelMeterRecorderServiceOptions>>().Value);
        builder.Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<AudioCardOptions>>().Value);
        builder.Logging.ClearProviders();
        builder.Services.AddSNRReductionWorker();
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        
        builder.Logging.AddFilter("Microsoft.Extensions.Hosting", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.Extensions.Hosting.Lifetime", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.Extensions.Hosting.Host", LogLevel.Warning);
        builder.Logging.AddFilter("Lifetime", LogLevel.Warning);
        builder.Logging.AddFilter("Host", LogLevel.Warning);

        builder.Services.AddAudioService(options =>
        {
            var snrOptions = builder.Configuration.GetSection(SNRReductionServiceOptions.Settings);

        });
        
       builder.Logging.AddNLog().AddNLogConfiguration().AddNlogFactoryAdaptor();
        builder.Services.AddHostedService<SNRReductionWorker>();

        var host = builder.Build();

        var logger = host.Services.GetRequiredService<ILog<Program>>();
        logger.Info("Application starting....");
        host.Run();
        logger.Info("Application finished.");

    }

}

public static class ServiceExtensions
{
    public static IServiceCollection AddSNRReductionWorker(this IServiceCollection services)
    {
        // Worker is registered as a hosted service elsewhere; keep extension minimal.
        services.AddSingleton(typeof(ILog<>), typeof(NLogAdapter<>));
        services.AddSingleton<IValidateOptions<SNRReductionServiceOptions>, SNRReductionOptionsValidationService>();
        services.AddSingleton<SNRReductionWorker>();
        return services;
    }
    
}
