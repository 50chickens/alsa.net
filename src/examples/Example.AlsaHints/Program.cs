using Example.AlsaHints;

class Program
{
    static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        // Prevent the host and framework from writing log lines to stdout
        builder.Logging.ClearProviders();
        builder.Services.AddHostedService<AlsaHintWorker>();
        builder.Services.AddAlsaHintService(services =>
        {
                        //fix this error:
            ///home/pistomp/alsa.net/src/examples/Example.AlsaHints/Program.cs(11,43): error CS7036: There is no argument given that corresponds to the required parameter 'services' of 'AlsaHintServiceBuilder.Build(IServiceProvider)'
            /// 


            return AlsaHintServiceBuilder.Build(services); //

        } );
        var host = builder.Build();        
        host.Run();
    }
}
