using AlsaSharp.Console.Consolonia.ViewModels;

namespace AlsaSharp.Console.Consolonia.Services;

public interface INavigationService
{
    ViewModelBase CurrentViewModel { get; }
    void NavigateTo<T>() where T : ViewModelBase;
    event EventHandler<ViewModelBase>? CurrentViewModelChanged;
}
