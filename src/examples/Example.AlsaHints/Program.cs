using AlsaSharp.Library.Extensions;
using AlsaSharp.Library.Logging;
using Microsoft.Extensions.Logging;

namespace Example.AlsaHints
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<AlsaHintWorker>();
            builder.Services.AddHintService(HintServiceBuilder.Build);
            // Wire up ILog<T> for dependency injection
            builder.Services.AddSingleton(typeof(ILog<>), typeof(LoggerAdapter<>));
            var host = builder.Build();
            host.Run();
        }
    }
}
