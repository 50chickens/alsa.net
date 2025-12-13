using AlsaSharp;
using AlsaSharp.Library.Builders;
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
            { "--AudioCardName", "SNRReduction:AudioCardName" },

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
            var recorder = serviceProvider.GetRequiredService<IAudioLevelMeterRecorderService>();
            return new SignalNoiseRatioOptimizer(log, opts, recorder);
        });
        builder.Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<AudioCardOptions>>().Value);
        builder.Services.AddSingleton<IValidateOptions<SNRReductionServiceOptions>, SNRReductionOptionsValidationService>();
        builder.Services.AddSingleton(typeof(ILog<>), typeof(NLogAdapter<>));
        builder.Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<AudioLevelMeterRecorderServiceOptions>>().Value);
        builder.Services.AddSingleton<IAudioLevelMeterRecorderService, AudioLevelMeterRecorderService>();
        builder.Services.AddSingleton<IAudioInterfaceLevelMeter>(serviceProvider =>
            new AudioInterfaceLevelMeter(
                serviceProvider.GetRequiredService<ISoundDevice>(),
                serviceProvider.GetRequiredService<ILog<AudioInterfaceLevelMeter>>()));
        builder.Services.AddSingleton(serviceProvider =>
        {
            SoundDeviceSettings soundDeviceSettings = new SoundDeviceSettings();
            return AlsaDeviceBuilder.Build(soundDeviceSettings);
        });
        builder.Services.Configure<AudioCardOptions>(builder.Configuration.GetSection(AudioCardOptions.Settings));
        builder.Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<SNRReductionServiceOptions>>().Value);
        builder.Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<AudioCardOptions>>().Value);
        builder.Logging.ClearProviders();

        builder.Logging.SetMinimumLevel(LogLevel.Trace);

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

        services.AddSingleton<IAudioLevelMeterRecorderService, AudioLevelMeterRecorderService>();
        services.AddSingleton(typeof(ILog<>), typeof(NLogAdapter<>));
        services.AddSingleton<IValidateOptions<SNRReductionServiceOptions>, SNRReductionOptionsValidationService>();
        services.AddSingleton<SNRReductionWorker>();
        return services;
    }
}
