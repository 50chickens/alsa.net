using Examples.SNRReduction.Interfaces;
using Examples.SNRReduction.Models;
using Examples.SNRReduction.Services;
using Example.SNRReduction.Logging;
using Microsoft.Extensions.Options;
using NLog.Extensions.Logging;
using AlsaSharp.Library.Logging;
using Example.SNRReduction.Audio;
using Example.SNRReduction.Extensions;
using Example.SNRReduction.Models;
using Example.SNRReduction.Services;

namespace Example.SNRReduction;
internal class Program
{
    private static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        // CreateApplicationBuilder has already:
        // Set the content root to the path returned by GetCurrentDirectory().
        // Loaded host configuration from:
        //   Environment variables: AddEnvironmentVariables(prefix: "DOTNET_").
        //   Command line arguments: AddCommandLine(args).
        //
        // Loaded app configuration from (for overriding app settings in order of lowest to highest priority):
        //   appsettings.json: AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).
        //   appsettings.{Environment}.json: AddJsonFile($"appsettings.{Environment}.json", optional: true, reloadOnChange: true).
        //   User secrets: AddUserSecrets(<assembly>, optional: true, reloadOnChange: true) if the current host environment is Development.
        //      i.e. In Visual Studio, right-click on the project and select 'Manage User Secrets'.
        //   Environment variables: AddEnvironmentVariables(), so app settings can be overridden using environment variables.
        //      e.g. Variable name: weatherForecast:windSpeedUnit / Variable value: MPH
        //   Command line arguments: AddCommandLine(args), so app settings can be overridden using CL arguments with nested app settings property names.
        //      e.g. <appname>.exe --weatherForecast:numberOfDays 7 --weatherForecast:temperatureScale F --weatherForecast:windSpeedUnit MPH


        // Allow app settings to be overridden via environment variables, e.g. WEATHER_weatherForecast:temperatureScale
        // NOTE: Calling AddEnvironmentVariables again is unnecessary if not prefixing variables, e.g. weatherForecast:temperatureScale
        builder.Configuration.AddEnvironmentVariables(prefix: "SNR_");

        // Because AddEnvironmentVariables has been called above (superseding command line arguments), call AddCommandLine again.
        // Allow app settings to be overridden via the command line using single dash or double dash arguments.
        // e.g. <appname>.exe --autosweep (to run in Development environment add '--environment Development').
        var switchMappings = new Dictionary<string, string>
        {
            { "--AutoSweep", "SNRReduction:AutoSweep" },
            { "--AudioCardName", "SNRReduction:AudioCardName" },
            { "--TerminalGui", "SNRReduction:TerminalGui" },
            { "--GuiIntervalSeconds", "SNRReduction:GuiIntervalSeconds" }
        };
        builder.Configuration.AddCommandLine(args, switchMappings);
        // NOTE: If AddCommandLine is called without switch mappings, command line arguments must match nested app settings property names.
        // e.g. <appname>.exe --SNRReduction:AutoSweep true --SNRReduction:AudioCardName "MyAudioCard"

        // Use the Options pattern to bind app settings.  Validation is performed in WeatherForecastOptionsValidation.
        builder.Services.AddOptions<SNRReductionServiceOptions>().Bind(builder.Configuration.GetSection(SNRReductionServiceOptions.Settings));

        builder.Services.AddOptions<AudioLevelMeterRecorderServiceOptions>().Bind(builder.Configuration.GetSection(AudioLevelMeterRecorderServiceOptions.Settings));
        builder.Services.AddSingleton<IValidateOptions<SNRReductionServiceOptions>, SNRReductionOptionsValidationService>();
        // Register SNRReductionOptions by delegating to IOptions object to remove IOptions dependency.
        //builder.Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<SNRReductionServiceOptions>>().Value);

        // Register generic logging adapter so DI can inject ILog<T> where needed
        builder.Services.AddSingleton(typeof(ILog<>), typeof(NLogAdapter<>));
        

//register audio level meter recorder service
        builder.Services.AddSingleton<IAudioLevelMeterRecorderService, AudioLevelMeterRecorderService>();

        builder.Services.AddSingleton<AudioInterfaceLevelMeter>();
        builder.Services.AddSingleton<IAudioInterfaceLevelMeter>(sp => sp.GetRequiredService<AudioInterfaceLevelMeter>());
        builder.Services.AddSingleton<SNRReductionApp>();
        builder.Services.AddSingleton<ISNRReductionService, SNRReductionService>();
        
        builder.Services.AddSingleton<TerminalGuiRunner>();
        builder.Services.AddSingleton<AudioLevelMeterRecorderService>();
        builder.Services.AddSingleton<AudioInterfaceLevelMeter>();
        
        // CreateApplicationBuilder has already added the Console, Debug, EventLog, and EventSource loggers.
        // Add any additional logging configuration to what is specified in appsettings.{Environment}.json.
        // e.g. builder.Logging.AddJsonConsole();
        
        builder.Logging.ClearProviders();
        // set host logging to Trace so we capture detailed sweep diagnostics
        builder.Logging.SetMinimumLevel(LogLevel.Trace);
        
        //inject configuration of the audio card name into the AudioService to be used by AudioInterfaceLevelMeter

        
        builder.Services.AddAudioService(options =>
        {
            var snrOptions = builder.Configuration.GetSection(SNRReductionServiceOptions.Settings);
                
        });
        builder.Logging.AddNLog().AddNLogConfiguration().AddNlogFactoryAdaptor();
        using var host = builder.Build();

        using var serviceScope = host.Services.CreateScope();
        var serviceProvider = serviceScope.ServiceProvider;

        var logger = serviceProvider.GetRequiredService<ILog<Program>>();
        var options = serviceProvider.GetRequiredService<SNRReductionServiceOptions>();
        // get an ILog<T> from DI (wrapped around ILogger<T>)

        logger.Info("Application starting...");

        // if (options.TerminalGui)
        // {
        //     // Run interactive terminal GUI mode
        //     serviceProvider.GetRequiredService<TerminalGuiRunner>().Run(options);
        // }
        // else
        // {
            serviceProvider.GetRequiredService<SNRReductionApp>().Run();
        //}

        logger.Info("Application finished.");
        
    }
    
}

