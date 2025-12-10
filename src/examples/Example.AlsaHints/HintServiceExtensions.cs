using AlsaSharp.Internal.Audio;

public static class HintServiceExtensions
{
    public static IServiceCollection AddHintService(this IServiceCollection services, Func<IServiceProvider, IHintService> implementationFactory)
    {
        services.AddSingleton(implementationFactory);
        return services;
    }
}
