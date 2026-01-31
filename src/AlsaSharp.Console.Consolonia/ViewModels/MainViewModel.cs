using System;
using System.Reactive;
using ReactiveUI;
using AlsaSharp.Console.Consolonia.Services;

namespace AlsaSharp.Console.Consolonia.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private ViewModelBase _currentView;

    public MainViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        
        // Subscribe to navigation changes
        _navigationService.CurrentViewModelChanged += (_, viewModel) =>
        {
            CurrentView = viewModel;
        };
        
        // Start with the main menu view
        _currentView = this;
        
        // Setup commands
        NavigateToLoopbackTestCommand = ReactiveCommand.Create(NavigateToLoopbackTest);
        NavigateToMainMenuCommand = ReactiveCommand.Create(NavigateToMainMenu);
        QuitCommand = ReactiveCommand.Create(Quit);
    }

    public ViewModelBase CurrentView
    {
        get => _currentView;
        set => this.RaiseAndSetIfChanged(ref _currentView, value);
    }

    public ReactiveCommand<Unit, Unit> NavigateToLoopbackTestCommand { get; }
    public ReactiveCommand<Unit, Unit> NavigateToMainMenuCommand { get; }
    public ReactiveCommand<Unit, Unit> QuitCommand { get; }

    private void NavigateToLoopbackTest()
    {
        _navigationService.NavigateTo<LoopbackTestViewModel>();
    }

    private void NavigateToMainMenu()
    {
        CurrentView = this;
    }

    private void Quit()
    {
        Environment.Exit(0);
    }
}
