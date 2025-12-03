using Examples.SNRReduction.Interfaces;
using Examples.SNRReduction.Models;
using Examples.SNRReduction.Services;
using Example.SNRReduction.Logging;
using Microsoft.Extensions.Options;
using NLog.Extensions.Logging;
using AlsaSharp.Library.Logging;

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
            { "--AudioCardName", "SNRReduction:AudioCardName" }
        };
        builder.Configuration.AddCommandLine(args, switchMappings);
        // NOTE: If AddCommandLine is called without switch mappings, command line arguments must match nested app settings property names.
        // e.g. <appname>.exe --SNRReduction:AutoSweep true --SNRReduction:AudioCardName "MyAudioCard"

        // Use the Options pattern to bind app settings.  Validation is performed in WeatherForecastOptionsValidation.
        builder.Services.AddOptions<SNRReductionOptions>().Bind(builder.Configuration.GetSection(SNRReductionOptions.Settings));
        builder.Services.AddSingleton<IValidateOptions<SNRReductionOptions>, SNRReductionOptionsValidationService>();
        // Register SNRReductionOptions by delegating to IOptions object to remove IOptions dependency.
        builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<SNRReductionOptions>>().Value);

        // Register generic logging adapter so DI can inject ILog<T> where needed
        builder.Services.AddSingleton(typeof(ILog<>), typeof(NLogAdapter<>));
        builder.Services.AddSingleton<SNRReductionApp>();
        builder.Services.AddSingleton<ISNRReductionService, SNRReductionService>();
        // CreateApplicationBuilder has already added the Console, Debug, EventLog, and EventSource loggers.
        // Add any additional logging configuration to what is specified in appsettings.{Environment}.json.
        // e.g. builder.Logging.AddJsonConsole();
        
        builder.Logging.ClearProviders();
        // add NLog provider, apply minimal programmatic config, and wire Common.Logging adapter
        builder.Logging.AddNLog().AddNLogConfiguration().AddNlogFactoryAdaptor();
        using var host = builder.Build();

        using var serviceScope = host.Services.CreateScope();
        var serviceProvider = serviceScope.ServiceProvider;

        var logger = serviceProvider.GetRequiredService<ILog<Program>>();
        // get an ILog<T> from DI (wrapped around ILogger<T>)

        logger.Info("Application starting...");
        serviceProvider.GetRequiredService<SNRReductionApp>().GetSNRReduction();
        logger.Info("Application finished.");
        
    }
}
