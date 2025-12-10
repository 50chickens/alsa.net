
public static class AlsaHintServiceBuilder
{
    internal static IAlsaHintService Build(IServiceProvider services)
    {
        var logger = services.GetService<ILogger<AlsaHintService>>();
        return new AlsaHintService(logger);
    }
}