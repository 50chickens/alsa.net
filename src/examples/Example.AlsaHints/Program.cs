using AlsaSharp.Library.Extensions;

namespace Example.AlsaHints
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<AlsaHintWorker>();
            builder.Services.AddHintService(HintServiceBuilder.Build);
            var host = builder.Build();
            host.Run();
        }
    }
}