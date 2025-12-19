using AlsaSharp;
using AlsaSharp.Library.Builders;
using AlsaSharp.Library.Extensions;
using AlsaSharp.Library.Logging;
using Example.SNRReduction.Audio;
using Example.SNRReduction.Extensions;
using Example.SNRReduction.Interfaces;
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

        builder.Services.AddSingleton<IControlSweepService>(serviceProvider =>
        {
            var log = serviceProvider.GetRequiredService<ILog<SignalNoiseRatioOptimizer>>();
            var opts = serviceProvider.GetService<ControlSweepOptions>() ?? new ControlSweepOptions(new List<AlsaControl>());
            return new SignalNoiseRatioOptimizer(log, opts);
        });
        builder.Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<AudioCardOptions>>().Value);
        builder.Services.AddSingleton<IValidateOptions<SNRReductionServiceOptions>, SNRReductionOptionsValidationService>();
        builder.Services.AddSingleton(typeof(ILog<>), typeof(NLogAdapter<>));
        builder.Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<AudioLevelMeterRecorderServiceOptions>>().Value);

        // Register the concrete AudioLevelMeterRecorderService so consumers can obtain
        // a recorder instance from DI. The recorder's measurement methods accept a
        // per-device `IAudioInterfaceLevelMeter` so it does not require a device
        // during construction.
        builder.Services.AddSingleton<IAudioLevelMeterRecorderService, AudioLevelMeterRecorderService>();

        // Register ALSA hint service and build ISoundDevice instances for all discovered cards
        var snrSection = builder.Configuration.GetSection(SNRReductionServiceOptions.Settings);
        var measurementFolder = snrSection.GetValue<string>("MeasurementFolder") ?? "~/.SNRReduction";
        builder.Services.AddUnixSoundDeviceBuilder(measurementFolder);
        builder.Services.AddSingleton<AudioCardConfigService>();
        builder.Services.AddSingleton<ISNRMeasurementService, SNRMeasurementService>();
        builder.Services.AddSingleton<ISNRWorkerHelper, SNRWorkerHelper>();
        builder.Services.AddSingleton<ILoopbackTester, LoopbackTester>();
        builder.Services.AddSingleton<ISNRMonitorService, SNRMonitorService>();

        builder.Services.Configure<AudioCardOptions>(builder.Configuration.GetSection(AudioCardOptions.Settings));
        builder.Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<SNRReductionServiceOptions>>().Value);
        builder.Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<AudioCardOptions>>().Value);
        builder.Logging.ClearProviders();

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
