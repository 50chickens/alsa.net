using System;
using AlsaSharp.Console.Consolonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AlsaSharp.Console.Consolonia.Services;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private ViewModelBase _currentViewModel;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _currentViewModel = null!;
    }

    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            _currentViewModel = value;
            CurrentViewModelChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<ViewModelBase>? CurrentViewModelChanged;

    public void NavigateTo<T>() where T : ViewModelBase
    {
        var viewModel = _serviceProvider.GetRequiredService<T>();
        CurrentViewModel = viewModel;
    }
}
