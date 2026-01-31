using Avalonia;
using Consolonia;
using Consolonia.Themes;
using AlsaSharp.Console.Consolonia;

class Program
{
    static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithConsoleLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UseConsolonia()
            .UseAutoDetectedConsole()
            .LogToException();
}
