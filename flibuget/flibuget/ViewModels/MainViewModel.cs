using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using flibuget.Core.Examples;
using Microsoft.Extensions.Logging;
using System;

namespace flibuget.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private string greeting = "Flibuget!";

    [ObservableProperty]
    private string author = string.Empty;

    [ObservableProperty]
    private string book = string.Empty;

    [ObservableProperty]
    private string result = string.Empty;

    public object ClickCommand { get; }

    public MainViewModel(ILogger<MainViewModel> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        
        _logger.LogInformation("MainViewModel initialized");
        _logger.LogDebug("Greeting message: {Greeting}", Greeting);
        
        // Example of structured logging with properties
        _logger.LogInformation("Application started for user {UserName} on machine {MachineName}", 
            Environment.UserName, 
            Environment.MachineName);

        ClickCommand = new RelayCommand(OnButtonClick);
    }

    private void OnButtonClick()
    {
        Result = $"Author: {Author}, Book: {Book}";
        PerplexityApiUsageExample.Example3_AudioBookStructuredJsonResponseAsync(_serviceProvider, Book, Author);
    }
}
