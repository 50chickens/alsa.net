using System;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using AlsaSharp.Console.Consolonia.Services;
using AlsaSharp.Library.Operations.Models;

namespace AlsaSharp.Console.Consolonia.ViewModels;

public class LoopbackTestViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private readonly INavigationService _navigationService;
    
    private string _cardName = "hw:0,0";
    private int _testDurationMs = 5000;
    private double _testLevelDbfs = -20.0;
    private bool _isRunning;
    private string _resultMessage = "";

    public LoopbackTestViewModel(IApiClient apiClient, INavigationService navigationService)
    {
        _apiClient = apiClient;
        _navigationService = navigationService;
        
        ExecuteTestCommand = ReactiveCommand.CreateFromTask(ExecuteTestAsync);
        BackCommand = ReactiveCommand.Create(GoBack);
    }

    public string CardName
    {
        get => _cardName;
        set => this.RaiseAndSetIfChanged(ref _cardName, value);
    }

    public int TestDurationMs
    {
        get => _testDurationMs;
        set => this.RaiseAndSetIfChanged(ref _testDurationMs, value);
    }

    public double TestLevelDbfs
    {
        get => _testLevelDbfs;
        set => this.RaiseAndSetIfChanged(ref _testLevelDbfs, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        set => this.RaiseAndSetIfChanged(ref _isRunning, value);
    }

    public string ResultMessage
    {
        get => _resultMessage;
        set => this.RaiseAndSetIfChanged(ref _resultMessage, value);
    }

    public ReactiveCommand<Unit, Unit> ExecuteTestCommand { get; }
    public ReactiveCommand<Unit, Unit> BackCommand { get; }

    private async Task ExecuteTestAsync()
    {
        try
        {
            IsRunning = true;
            ResultMessage = "Testing... Please wait.";

            var cardSelector = new CardSelector(
                CardName: CardName,
                DeviceName: null,
                CardNumber: null,
                DeviceNumber: null
            );

            var request = new LoopbackTestRequest(
                CardSelector: cardSelector,
                TestDurationMs: TestDurationMs,
                TestLevelDbfs: TestLevelDbfs
            );

            var response = await _apiClient.ExecuteLoopbackTestAsync(request);

            if (response.Success)
            {
                var result = response.Result;
                if (result != null)
                {
                    ResultMessage = $"Test completed successfully!\n\n" +
                                   $"Message: {response.Message}\n" +
                                   $"Samples: {result.Samples}\n" +
                                   $"Has Signal: {result.HasSignal}\n" +
                                   $"SNR: {result.SignalToNoiseRatio:F2} dB\n" +
                                   $"THD: {result.TotalHarmonicDistortionDb:F2} dB\n" +
                                   $"Latency: {result.RoundTripLatencyMs:F2} ms";
                }
                else
                {
                    ResultMessage = $"Test completed: {response.Message}";
                }
            }
            else
            {
                ResultMessage = $"Test failed: {response.Message}";
            }
        }
        catch (Exception ex)
        {
            ResultMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
        }
    }

    private void GoBack()
    {
        _navigationService.NavigateTo<MainViewModel>();
    }
}
