using Example.AlsaHints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddHostedService<AlsaHintWorker>();
        builder.Services.AddAlsaHintService(services =>
        {
            return AlsaHintServiceBuilder.Build();
        } );
        // Remove the default logging providers so the example prints only friendly output
        // (prevents the default prefix messages such as "info: Example.AlsaHints.AlsaHintWorker[0]")
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.None);
        var host = builder.Build();        
        host.Run();
    }

}

public static class AlsaHintServiceExtensions
{
    public static IServiceCollection AddAlsaHintService(this IServiceCollection services, Func<IServiceProvider, IAlsaHintService> implementationFactory)
    {
        services.AddSingleton(implementationFactory);
        return services;
    }
}