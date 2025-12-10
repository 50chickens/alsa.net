public static class AlsaHintServiceExtensions
{
    public static IServiceCollection AddAlsaHintService(this IServiceCollection services, Func<IServiceProvider, IAlsaHintService> implementationFactory)
    {
        services.AddSingleton(implementationFactory);
        return services;
    }
}
