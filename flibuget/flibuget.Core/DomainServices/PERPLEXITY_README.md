# Perplexity API Client for .NET

A comprehensive C# client for the Perplexity API, providing seamless integration with .NET applications.

## ?? Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Setup](#setup)
- [Usage Examples](#usage-examples)
- [API Reference](#api-reference)
- [Configuration](#configuration)
- [Error Handling](#error-handling)
- [Logging](#logging)
- [Best Practices](#best-practices)

## ? Features

- ? Chat completions with multiple Perplexity models
- ? Structured JSON responses with schema validation
- ? Temperature and max tokens control
- ? Full async/await support
- ? Comprehensive logging with Serilog integration
- ? Bearer token authentication
- ? Built on existing HttpService infrastructure
- ? Smart API key resolution (Configuration, Environment Variable, Default)
- ? Type-safe request/response models
- ? Cancellation token support

## ??? Architecture

### Components

#### Core Service
- **`AudiobookService`** - Main client for Perplexity API interactions

#### Models
- **`PerplexityMessage`** - Chat messages (role + content)
- **`PerplexityChatCompletionRequest`** - Request payload
- **`PerplexityChatCompletionResponse`** - Response payload
- **`PerplexityJsonSchema`** - Structured response schema definitions
- **`PerplexityResponseFormat`** - Response format configuration
- **`PerplexityUsage`** - Token usage information

#### Configuration
- **`PerplexityServiceExtensions`** - Dependency injection extensions

## ?? Setup

### Step 1: Configure Dependency Injection

Add the Perplexity service to your DI container in `CompositionRoot.cs` or `Program.cs`:

```csharp
// Required services
services.AddHttpClient();
services.AddTransient<IWebService, HttpService>();

// Add Perplexity service with automatic API key resolution
services.AddPerplexityService(configuration);
```

### Step 2: Configure API Key

The service uses intelligent API key resolution with the following priority:

#### Resolution Logic

```
1. Read Perplexity:ApiKey from configuration (appsettings.json)
2. If value equals "your-perplexity-api-key-here" (placeholder) OR is empty/missing:
   ? Use environment variable: FLIBUGET_AI_APIKEY
3. Otherwise:
   ? Use the configuration value as-is
```

#### Option A: Production Configuration (Recommended)

Use actual API key in configuration:

```json
{
  "Perplexity": {
    "ApiKey": "pplx-your-actual-api-key-here"
  }
}
```

**Result:** ? Uses configuration value directly

#### Option B: Development with Environment Variable

Keep placeholder in configuration for safety:

```json
{
  "Perplexity": {
    "ApiKey": "your-perplexity-api-key-here"
  }
}
```

Set environment variable:

```powershell
# Windows PowerShell
$env:FLIBUGET_AI_APIKEY="pplx-your-actual-api-key-here"

# Windows Command Prompt
set FLIBUGET_AI_APIKEY=pplx-your-actual-api-key-here

# Linux/macOS
export FLIBUGET_AI_APIKEY="pplx-your-actual-api-key-here"
```

**Result:** ? Uses environment variable `FLIBUGET_AI_APIKEY`

#### Option C: Environment Variable Only

Remove or leave empty in configuration:

```json
{
  "Database": {
    "ConnectionString": "Data Source=flibuget.db"
  }
}
```

Set environment variable (see Option B).

**Result:** ? Uses environment variable `FLIBUGET_AI_APIKEY`

### Benefits of This Approach

- ? **Security**: Never commit real API keys to source control
- ? **Flexibility**: Different keys for dev/staging/production
- ? **CI/CD Ready**: Easy integration with deployment pipelines
- ? **Safe Defaults**: Placeholder value prevents accidental commits

## ?? Usage Examples

See `PerplexityApiUsageExample.cs` for complete, runnable examples.

### Example 1: Simple Question & Answer

```csharp
var service = App.ServiceProvider.GetRequiredService<AudiobookService>();

var answer = await service.AskAsync(
    "What is the capital of France?",
 model: "sonar-pro");

Console.WriteLine($"Answer: {answer}");
```

### Example 2: Multi-turn Conversation

```csharp
var messages = new List<PerplexityMessage>
{
    new() { Role = "system", Content = "You are a helpful coding assistant." },
  new() { Role = "user", Content = "Explain dependency injection in C#." }
};

var response = await service.CreateChatCompletionAsync(
    messages,
    model: "sonar-pro",
    temperature: 0.7,
    maxTokens: 500);

Console.WriteLine($"Response: {response.Choices[0].Message.Content}");
Console.WriteLine($"Tokens used: {response.Usage?.TotalTokens}");
```

### Example 3: Structured JSON Response

Define a schema and get structured data:

```csharp
// Define schema
var schema = new PerplexityJsonSchema
{
    Type = "object",
  Properties = new Dictionary<string, object>
  {
        ["companies"] = new Dictionary<string, object>
        {
            ["type"] = "array",
     ["items"] = new Dictionary<string, object>
   {
                ["type"] = "object",
 ["properties"] = new Dictionary<string, object>
   {
                ["name"] = new Dictionary<string, string> { ["type"] = "string" },
             ["founded"] = new Dictionary<string, string> { ["type"] = "string" },
       ["industry"] = new Dictionary<string, string> { ["type"] = "string" }
        },
    ["required"] = new[] { "name", "founded", "industry" }
          }
        }
    },
 Required = new List<string> { "companies" }
};

// Request structured data
var completion = await service.CreateStructuredCompletionAsync(
    "List 3 major tech companies founded after 2000. Include name, founding year, and primary industry.",
    schema,
    model: "sonar-pro");

// Parse response
var result = JsonSerializer.Deserialize<CompanyResponse>(
    completion.Choices[0].Message.Content);

foreach (var company in result.Companies)
{
    Console.WriteLine($"{company.Name} ({company.Founded}) - {company.Industry}");
}
```

### Example 4: With Cancellation Token

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    var response = await service.AskAsync(
        "Explain quantum computing",
        model: "sonar-pro",
        cancellationToken: cts.Token);
    
    Console.WriteLine(response);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Request was cancelled");
}
```

## ?? API Reference

### AudiobookService

#### Constructor

```csharp
public AudiobookService(
    IWebService webService,
    ILogger<AudiobookService> logger,
    string apiKey)
```

#### Methods

##### CreateChatCompletionAsync

Full control over chat completion requests.

```csharp
Task<PerplexityChatCompletionResponse> CreateChatCompletionAsync(
    List<PerplexityMessage> messages,
    string model = "sonar-pro",
    PerplexityResponseFormat? responseFormat = null,
    double? temperature = null,
    int? maxTokens = null,
    CancellationToken cancellationToken = default)
```

**Parameters:**
- `messages` - List of conversation messages
- `model` - Perplexity model to use (default: "sonar-pro")
- `responseFormat` - Optional structured response configuration
- `temperature` - Optional temperature (0.0 - 1.0)
- `maxTokens` - Optional maximum tokens to generate
- `cancellationToken` - Cancellation token

**Returns:** Complete response with choices and usage information

##### AskAsync

Simple single-question helper method.

```csharp
Task<string> AskAsync(
    string userMessage,
    string model = "sonar-pro",
    CancellationToken cancellationToken = default)
```

**Parameters:**
- `userMessage` - The question or prompt
- `model` - Perplexity model to use
- `cancellationToken` - Cancellation token

**Returns:** String content of the first response choice

##### CreateStructuredCompletionAsync

Request structured JSON responses with schema validation.

```csharp
Task<PerplexityChatCompletionResponse> CreateStructuredCompletionAsync(
    string userMessage,
    PerplexityJsonSchema jsonSchema,
    string model = "sonar-pro",
    CancellationToken cancellationToken = default)
```

**Parameters:**
- `userMessage` - The question or prompt
- `jsonSchema` - JSON schema defining the expected response structure
- `model` - Perplexity model to use
- `cancellationToken` - Cancellation token

**Returns:** Response with structured JSON content matching the schema

## ?? Configuration

### Configuration Keys

| Key | Description | Default |
|-----|-------------|---------|
| `Perplexity:ApiKey` | API key in appsettings.json | Required |

### Environment Variables

| Variable | Description | Used When |
|----------|-------------|-----------|
| `FLIBUGET_AI_APIKEY` | API key from environment | Config has placeholder or is missing |

### Special Values

| Value | Behavior |
|-------|----------|
| `"your-perplexity-api-key-here"` | Treated as placeholder ? uses environment variable |
| Empty or missing | Uses environment variable |
| Any other value | Uses configuration value directly |

## ?? Supported Models

| Model | Description | Use Case |
|-------|-------------|----------|
| `sonar-pro` | Most capable model | Complex queries, detailed answers (default) |
| `sonar` | Faster, cost-effective | Simple queries, quick responses |

Refer to [Perplexity API documentation](https://docs.perplexity.ai/) for the latest model availability.

## ?? Error Handling

The service throws the following exceptions:

| Exception | Cause |
|-----------|-------|
| `ArgumentNullException` | Required parameter is null |
| `ArgumentException` | Invalid parameter value (e.g., empty API key) |
| `HttpRequestException` | API request failure |
| `TimeoutException` | Request timeout (default: 240 seconds) |
| `InvalidOperationException` | Response processing failure |

### Example Error Handling

```csharp
try
{
    var response = await service.AskAsync("Your question here");
    Console.WriteLine(response);
}
catch (ArgumentException ex)
{
    _logger.LogError(ex, "Invalid argument");
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "API request failed");
}
catch (TimeoutException ex)
{
    _logger.LogError(ex, "Request timed out");
}
```

## ?? Logging

The service integrates with `ILogger<AudiobookService>` for comprehensive logging:

### Log Levels

| Level | Events Logged |
|-------|---------------|
| **Information** | API calls, responses, configuration |
| **Debug** | Detailed request/response data |
| **Error** | Exceptions and failures |

### Example Log Output

```
[INFO] Perplexity API service configured with API key
[INFO] Sending chat completion request to Perplexity API with model sonar-pro
[DEBUG] Received response from Perplexity API
```

Configure logging in `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
  "Override": {
        "flibuget.Core.DomainServices.AudiobookService": "Debug"
      }
    }
  }
}
```

## ?? Best Practices

### 1. API Key Management

```csharp
// ? DO: Use configuration or environment variables
services.AddPerplexityService(configuration);

// ? DON'T: Hardcode API keys
var service = new AudiobookService(webService, logger, "pplx-hardcoded-key");
```

### 2. Use Appropriate Models

```csharp
// ? DO: Use sonar-pro for complex queries
await service.AskAsync("Analyze the implications of...", model: "sonar-pro");

// ? DO: Use sonar for simple queries
await service.AskAsync("What is 2+2?", model: "sonar");
```

### 3. Handle Cancellation

```csharp
// ? DO: Pass cancellation tokens
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
await service.AskAsync(question, cancellationToken: cts.Token);
```

### 4. Dispose Resources Properly

```csharp
// ? DO: Use dependency injection (automatically managed)
var service = serviceProvider.GetRequiredService<AudiobookService>();

// ? DO: Use using statements for manual instantiation
using var scope = serviceProvider.CreateScope();
var service = scope.ServiceProvider.GetRequiredService<AudiobookService>();
```

### 5. Log Appropriately

```csharp
// ? DO: Log business events
_logger.LogInformation("Processing user query: {Query}", userQuery);

// ? DON'T: Log sensitive data
_logger.LogDebug("API Key: {ApiKey}", apiKey); // Never do this!
```

## ?? Related Files

- **`AudiobookService.cs`** - Main service implementation
- **`PerplexityModels.cs`** - Request/response models
- **`PerplexityServiceExtensions.cs`** - DI configuration
- **`PerplexityApiUsageExample.cs`** - Complete usage examples
- **`CompositionRoot.cs`** - Application DI setup

## ?? License

This implementation follows the same license as the flibuget project (MIT).

## ?? Contributing

When contributing to the Perplexity integration:

1. Follow existing code patterns
2. Add comprehensive logging
3. Include XML documentation comments
4. Add unit tests for new features
5. Update this README with new examples

---

**Note:** This client is designed specifically for the flibuget application but can be adapted for other .NET projects.
