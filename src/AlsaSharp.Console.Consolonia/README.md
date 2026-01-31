# AlsaSharp Console Consolonia Application

A terminal-based GUI application for AlsaSharp audio testing using Avalonia and Consolonia frameworks with MVVM pattern.

## Features

- **Main Menu**: Navigate between different audio test operations
- **Loopback Test**: Fully implemented test view with form inputs and result display
- **Additional Tests**: Placeholders for SNR Test, Test Tone, and Audio Levels (TODO)

## Architecture

### Project Structure

```
AlsaSharp.Console.Consolonia/
├── Services/
│   ├── IApiClient.cs           # HTTP client interface for API calls
│   ├── ApiClient.cs            # HTTP client implementation
│   ├── INavigationService.cs   # Navigation service interface
│   └── NavigationService.cs    # Navigation service implementation
├── ViewModels/
│   ├── ViewModelBase.cs        # Base ReactiveUI view model
│   ├── MainViewModel.cs        # Main menu view model with navigation
│   └── LoopbackTestViewModel.cs # Loopback test view model
├── Views/
│   ├── MainWindow.axaml[.cs]   # Main window container
│   ├── MainView.axaml[.cs]     # Main menu view
│   └── LoopbackTestView.axaml[.cs] # Loopback test view
├── App.axaml[.cs]              # Application entry with DI configuration
├── Program.cs                  # Program entry point
└── appsettings.json            # Configuration settings
```

### Technologies Used

- **Avalonia UI 11.3.9**: Cross-platform XAML-based UI framework
- **Consolonia**: Console-based rendering for Avalonia
- **ReactiveUI**: MVVM framework with reactive extensions
- **Microsoft.Extensions.DependencyInjection**: Dependency injection
- **Microsoft.Extensions.Http**: HTTP client factory

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run
```

## Configuration

The API base URL can be configured in `appsettings.json`:

```json
{
  "ApiBaseUrl": "http://localhost:5000"
}
```

## Usage

1. **Main Menu**: Use arrow keys to navigate and Enter to select
2. **Loopback Test**:
   - Enter card name (e.g., "hw:0,0")
   - Set test duration in milliseconds
   - Set test level in dBFS
   - Click "Execute Test" to run
   - Results will display in the text area
   - Click "Back to Menu" to return

## TODO

The following test views need to be implemented:

- [ ] SNR Test View and ViewModel
- [ ] Test Tone View and ViewModel
- [ ] Audio Levels View and ViewModel

To add these:
1. Create ViewModels similar to `LoopbackTestViewModel.cs`
2. Create Views similar to `LoopbackTestView.axaml`
3. Register ViewModels in `App.axaml.cs` ConfigureServices method
4. Add DataTemplates in `MainWindow.axaml`
5. Add menu items in `MainView.axaml`
6. Add navigation commands in `MainViewModel.cs`

## Notes

- Uses file-scoped namespaces for cleaner code
- ReactiveUI provides INotifyPropertyChanged through ReactiveObject
- All async operations use ReactiveCommand.CreateFromTask
- Error messages are displayed in the result areas
- Navigation is handled through a centralized NavigationService
