using AlsaSharp.Internal.Audio;

namespace Example.AlsaHints
{
    public static class HintServiceBuilder
    {
        internal static IHintService Build(IServiceProvider services)
        {
            var logger = services.GetService<ILogger<HintService>>();
            return new HintService(logger);
        }
    }
}