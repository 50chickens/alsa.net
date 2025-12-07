using System.Text.RegularExpressions;
using AlsaSharp;
using AlsaSharp.Internal;
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
    //     var builder = Host.CreateApplicationBuilder(args);
        
    //     builder.Configuration.AddEnvironmentVariables(prefix: "SNR_");

    //     var switchMappings = new Dictionary<string, string>
    //     {
    //         { "--AutoSweep", "SNRReduction:AutoSweep" },
    //         { "--AudioCardName", "SNRReduction:AudioCardName" },
    //         { "--TerminalGui", "SNRReduction:TerminalGui" },
    //         { "--GuiIntervalSeconds", "SNRReduction:GuiIntervalSeconds" }
    //     };

    //     builder.Configuration.AddCommandLine(args, switchMappings);
    //     builder.Services.AddOptions<SNRReductionServiceOptions>().Bind(builder.Configuration.GetSection(SNRReductionServiceOptions.Settings));
    //     builder.Services.AddOptions<AudioLevelMeterRecorderServiceOptions>().Bind(builder.Configuration.GetSection(AudioLevelMeterRecorderServiceOptions.Settings));
    //     builder.Services.AddOptions<AudioCardOptions>().Bind(builder.Configuration.GetSection(AudioCardOptions.Settings));
    //     builder.Services.AddSingleton<IAudioCardManager, AudioCardService>();
    //     builder.Services.AddSingleton<IControlSweepService, SignalNoiseRatioOptimizer>();
    //     builder.Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<AudioCardOptions>>().Value);
    //     builder.Services.AddSingleton<IValidateOptions<SNRReductionServiceOptions>, SNRReductionOptionsValidationService>();
    //     builder.Services.AddSingleton(typeof(ILog<>), typeof(NLogAdapter<>));
    //     builder.Services.AddSingleton<IAudioLevelMeterRecorderService, AudioLevelMeterRecorderService>();
    //     builder.Services.Configure<AudioCardOptions>(builder.Configuration.GetSection(AudioCardOptions.Settings));
    //     builder.Services.AddSingleton(serviceProvider =>
    //     {
    //         return AlsaDeviceBuilder.Build(new AudioCardService(serviceProvider.GetRequiredService<AudioCardOptions>().AudioCardName).GetAudioCards().FirstOrDefault());
    //     });
        
    //     builder.Services.AddSingleton<IAudioInterfaceLevelMeter>(serviceProvider =>
    //         new AudioInterfaceLevelMeter(
    //             serviceProvider.GetRequiredService<ISoundDevice>(),
    //             serviceProvider.GetRequiredService<ILog<AudioInterfaceLevelMeter>>()));

        
    //     builder.Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<SNRReductionServiceOptions>>().Value);
    //     builder.Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<AudioLevelMeterRecorderServiceOptions>>().Value);
    //     builder.Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<AudioCardOptions>>().Value);
    //     builder.Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IAudioInterfaceLevelMeter>());



    //     builder.Services.AddSingleton<SNRReductionApp>();
    //     builder.Services.AddSingleton<IControlSweepService, SignalNoiseRatioOptimizer>();
    //     builder.Services.AddSingleton<TerminalGuiRunner>();
        
    //     builder.Logging.ClearProviders();
        
    //     builder.Logging.SetMinimumLevel(LogLevel.Trace);
        
    //     builder.Services.AddAudioService(options =>
    //     {
    //         var snrOptions = builder.Configuration.GetSection(SNRReductionServiceOptions.Settings);
                
    //     });

    //     builder.Logging.AddNLog().AddNLogConfiguration().AddNlogFactoryAdaptor();
    //     using var host = builder.Build();

    //     using var serviceScope = host.Services.CreateScope();
    //     var serviceProvider = serviceScope.ServiceProvider;

    //     var logger = serviceProvider.GetRequiredService<ILog<Program>>();
    //     var options = serviceProvider.GetRequiredService<SNRReductionServiceOptions>();
    //     // get an ILog<T> from DI (wrapped around ILogger<T>)

    //     logger.Info("Application starting...");

    //     // if (options.TerminalGui)
    //     // {
    //     //     // Run interactive terminal GUI mode
    //     //     serviceProvider.GetRequiredService<TerminalGuiRunner>().Run(options);
    //     // }
    //     // else
    //     // {
    //         serviceProvider.GetRequiredService<SNRReductionApp>().Run();
    //     //}

    //     logger.Info("Application finished.");
        
    // }
    
}
}

