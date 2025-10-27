# Serilog Logging Configuration

This application uses Serilog for logging with configuration support from `appsettings.json`.

## Features

- **Configurable from appsettings.json**: All logging settings can be modified without recompiling
- **Environment-Specific Settings**: Automatic loading of Development/Production configurations
- **Multiple Sinks**: Logs to Console and File simultaneously
- **Plain Text Logs**: Human-readable logs in `logs/flibuget-{date}.txt`
- **Structured JSON Logs**: Machine-parseable JSON logs in `logs/flibuget-{date}.json`
- **Rolling Files**: Log files are created daily with date in filename
- **Dependency Injection**: ILogger<T> is available throughout the application

## Configuration

The application automatically loads configuration based on the environment:

### Environment Detection

1. **Debug Builds**: Automatically uses `Development` environment
2. **Release Builds**: Defaults to `Production` environment
3. **Environment Variable Override**: Set `DOTNET_ENVIRONMENT` or `ASPNETCORE_ENVIRONMENT` to override

Configuration files are loaded in order:
1. `appsettings.json` (base configuration)
2. `appsettings.{Environment}.json` (environment-specific overrides)

### Development Environment (`appsettings.Development.json`)

The development configuration includes:
- **Debug Level Logging**: More verbose output for debugging
- **More Framework Logs**: Shows Microsoft, System, and Avalonia logs at Information level
- **Enhanced Console Output**: Includes source context for easier debugging
- **Detailed File Logs**: Includes all properties in text format
- **Separate Log Directory**: `logs/development/` to keep dev logs separate
- **7-Day Retention**: Automatically cleans up old log files

Example console output in Development:
```
[14:23:45 DBG] flibuget.ViewModels.MainViewModel
  Greeting message: Flibuget!
```

### Production Environment (`appsettings.json`)

The production configuration includes:
- **Information Level Logging**: Only important messages
- **Minimal Framework Logs**: Microsoft and System at Warning level only
- **Clean Console Output**: Simplified format
- **Standard File Logs**: Essential information only
- **Main Log Directory**: `logs/`

### Log Levels

Available levels (from least to most verbose):
- `Verbose` - Very detailed logs
- `Debug` - Debugging information (Development only)
- `Information` - General informational messages (default)
- `Warning` - Warning messages
- `Error` - Error messages
- `Fatal` - Critical failures

```json
"MinimumLevel": {
  "Default": "Information",
  "Override": {
    "Microsoft": "Warning",
    "System": "Warning"
  }
}
```

### Output Formats

#### Console Output (Plain Text)
Logs to console with timestamp, level, and message:

**Production:**
```
[14:23:45 INF] MainViewModel initialized
```

**Development:**
```
[14:23:45 DBG] flibuget.ViewModels.MainViewModel
  Greeting message: Flibuget!
```

#### Text File Output (Plain Text)
Logs to `logs/flibuget-{date}.txt` (production) or `logs/development/flibuget-{date}.txt` (development) with full timestamp:

**Production:**
```
[2024-01-15 14:23:45.123 +00:00 INF] MainViewModel initialized
```

**Development:**
```
[2024-01-15 14:23:45.123 +00:00 DBG] flibuget.ViewModels.MainViewModel
Greeting message: Flibuget!
{ SourceContext: "flibuget.ViewModels.MainViewModel", Greeting: "Flibuget!" }
```

#### JSON File Output (Structured)
Logs to `logs/flibuget-{date}.json` with structured data:
```json
{"@t":"2024-01-15T14:23:45.123Z","@mt":"MainViewModel initialized","@l":"Information"}
```

### Switching Between Plain Text and JSON

To disable JSON logging, remove the third WriteTo entry from `appsettings.json`:
```json
"WriteTo": [
  { "Name": "Console", ... },
  { "Name": "File", ... }
  // Remove or comment out the JSON file sink
]
```

To use only JSON logging, remove the text file sink and keep only the JSON one.

## Usage in Code

### Inject ILogger into ViewModels or Services

```csharp
public class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel> _logger;

    public MainViewModel(ILogger<MainViewModel> logger)
  {
        _logger = logger;
 
        // Simple logging
    _logger.LogInformation("MainViewModel initialized");
     
        // Structured logging with properties
        _logger.LogInformation("User {UserName} performed action {ActionName}", 
userName, actionName);
    }
}
```

### Log Levels in Code

```csharp
_logger.LogTrace("Very detailed trace message");
_logger.LogDebug("Debug information");
_logger.LogInformation("General information");
_logger.LogWarning("Warning message");
_logger.LogError(exception, "Error occurred while {Action}", action);
_logger.LogCritical(exception, "Critical failure");
```

### Structured Logging Best Practices

Use message templates with property placeholders instead of string interpolation:

**Good:**
```csharp
_logger.LogInformation("Processing order {OrderId} for customer {CustomerId}", 
    orderId, customerId);
```

**Avoid:**
```csharp
_logger.LogInformation($"Processing order {orderId} for customer {customerId}");
```

The first approach creates structured properties that can be queried in JSON logs.

## Log File Location

### Production Environment
Logs are stored in the `logs/` directory:
- `logs/flibuget-20240115.txt` - Plain text log for January 15, 2024
- `logs/flibuget-20240115.json` - JSON log for January 15, 2024

### Development Environment
Logs are stored in the `logs/development/` directory:
- `logs/development/flibuget-20240115.txt` - Plain text log with verbose details
- `logs/development/flibuget-20240115.json` - JSON log with all debug information

Logs automatically roll over at midnight, creating new files each day. Development logs are retained for 7 days.

## Environment Configuration

### Setting the Environment

**Option 1: Environment Variable (recommended for production)**
```powershell
# Windows PowerShell
$env:DOTNET_ENVIRONMENT = "Production"

# Windows CMD
set DOTNET_ENVIRONMENT=Production

# Linux/macOS
export DOTNET_ENVIRONMENT=Production
```

**Option 2: Automatic (default behavior)**
- Debug builds ? Development environment
- Release builds ? Production environment

### Creating Custom Environments

Create additional environment configurations like `appsettings.Staging.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
    "Default": "Information"
    },
    "WriteTo": [
   {
   "Name": "Seq",
        "Args": { "serverUrl": "http://staging-seq-server:5341" }
      }
    ]
  }
}
```

Then set the environment variable:
```powershell
$env:DOTNET_ENVIRONMENT = "Staging"
```

## Advanced Configuration

### Add Additional Sinks

You can add more sinks by installing additional Serilog packages:

**Database Logging:**
```bash
dotnet add package Serilog.Sinks.MSSqlServer
```

**Seq (structured log server):**
```bash
dotnet add package Serilog.Sinks.Seq
```

**Email:**
```bash
dotnet add package Serilog.Sinks.Email
```

Then add them to appsettings.json WriteTo array.

### File Size Limits

Add file size limits to prevent large log files:
```json
{
  "Name": "File",
  "Args": {
    "path": "logs/flibuget-.txt",
    "rollingInterval": "Day",
    "fileSizeLimitBytes": 10485760,  // 10MB
    "rollOnFileSizeLimit": true,
    "retainedFileCountLimit": 7  // Keep last 7 days
  }
}
```

### Per-Namespace Log Levels

Control logging verbosity for specific namespaces:
```json
"MinimumLevel": {
  "Default": "Information",
  "Override": {
    "Microsoft": "Warning",
    "System": "Warning",
    "flibuget.Services": "Debug",
    "flibuget.ViewModels": "Information"
  }
}
```

### Troubleshooting

**Logs not appearing?**
1. Check the environment - run in Debug mode or set `DOTNET_ENVIRONMENT=Development`
2. Verify log level - Debug logs won't show with Information level
3. Check file permissions on the `logs/` directory
4. Look for Serilog self-diagnostic messages in the console

**Too many logs?**
1. Increase the minimum level in appsettings.json
2. Add overrides for noisy namespaces
3. Remove Debug-level logging from production

**Environment not detected correctly?**
1. Check `DOTNET_ENVIRONMENT` or `ASPNETCORE_ENVIRONMENT` variable
2. Verify the correct appsettings file exists
3. Check console output for "Application services configured successfully for environment: {Environment}"
