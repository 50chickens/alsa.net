using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using AlsaSharp.Console.Consolonia.Services;
using AlsaSharp.Console.Consolonia.ViewModels;
using AlsaSharp.Console.Consolonia.Views;

namespace AlsaSharp.Console.Consolonia;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = ConfigureServices();
            desktop.MainWindow = new MainWindow
            {
                DataContext = services.GetRequiredService<MainViewModel>()
            };
        }
        base.OnFrameworkInitializationCompleted();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        
        // Register HttpClient
        services.AddHttpClient<IApiClient, ApiClient>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:5000");
        });
        
        // Register ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<LoopbackTestViewModel>();
        // TODO: Add other ViewModels (SNRTestViewModel, TestToneViewModel, AudioLevelsViewModel)
        
        // Register Services
        services.AddSingleton<INavigationService, NavigationService>();
        
        return services.BuildServiceProvider();
    }
}
