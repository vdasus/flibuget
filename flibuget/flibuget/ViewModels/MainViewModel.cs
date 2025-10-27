using System;
using Microsoft.Extensions.Logging;

namespace flibuget.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel> _logger;

    public string Greeting => "Flibuget!";

    public MainViewModel(ILogger<MainViewModel> logger)
    {
        _logger = logger;
        
        _logger.LogInformation("MainViewModel initialized");
        _logger.LogDebug("Greeting message: {Greeting}", Greeting);
        
        // Example of structured logging with properties
        _logger.LogInformation("Application started for user {UserName} on machine {MachineName}", 
            Environment.UserName, 
            Environment.MachineName);
    }
}
