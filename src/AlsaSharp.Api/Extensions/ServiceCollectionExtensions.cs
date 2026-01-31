using AlsaSharp.Library.Operations.Services;
using AlsaSharp.Library.Services;
using AlsaSharp.Library.Logging;

namespace AlsaSharp.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOperations(this IServiceCollection services, IConfiguration configuration)
    {
        // Register logging adapter
        services.AddSingleton(typeof(ILog<>), typeof(LoggerAdapter<>));
        
        // Configure options
        services.Configure<LoopbackTestOptions>(configuration.GetSection(LoopbackTestOptions.Settings));
        services.AddSingleton(sp => new AudioLevelMeterRecorderServiceOptions());
        
        // Register all operation services as scoped
        services.AddScoped<ILoopbackTestOperation, LoopbackTestOperation>();
        services.AddScoped<ISNRTestOperation, SNRTestOperation>();
        services.AddScoped<ITestToneOperation, TestToneOperation>();
        services.AddScoped<IAudioLevelsOperation, AudioLevelsOperation>();
        services.AddScoped<ICopyOnlyOperation, CopyOnlyOperation>();
        services.AddScoped<IAudioCardsOperation, AudioCardsOperation>();
        
        // Register underlying services
        services.AddScoped<IAlsaLoopbackTestService, AlsaLoopbackTestService>();
        services.AddScoped<ISNRMeasurementService, SNRMeasurementService>();
        services.AddScoped<IAudioRecorderService, AudioRecorderService>();
        services.AddScoped<IAudioLevelMeterRecorderService, AudioLevelMeterRecorderService>();
        services.AddScoped<ICopyOnlyService, CopyOnlyService>();
        services.AddScoped<ITestToneService, TestToneService>();
        services.AddScoped<IAudioCardSelector, AudioCardSelector>();
        
        return services;
    }
    
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }
}
