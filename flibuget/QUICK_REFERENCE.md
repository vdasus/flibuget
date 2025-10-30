# AI Infrastructure Quick Reference

## Quick Start

### 1. Install Package

Already installed: `Azure.AI.OpenAI` v2.1.0

### 2. Configure API Keys

**Option A: Environment Variables (Recommended)**
```bash
# Windows PowerShell
$env:OPENAI_API_KEY = "sk-your-key-here"
$env:FLIBUGET_AI_APIKEY = "pplx-your-key-here"

# Linux/Mac/WSL
export OPENAI_API_KEY="sk-your-key-here"
export FLIBUGET_AI_APIKEY="pplx-your-key-here"
```

**Option B: appsettings.json (Development only)**
```json
{
  "AI": {
    "DefaultProvider": "OpenAI"
  },
  "OpenAI": {
    "ApiKey": "sk-your-actual-key-here",
    "DefaultModel": "gpt-4o-mini"
  },
  "Perplexity": {
    "ApiKey": "pplx-your-actual-key-here",
    "DefaultModel": "sonar-pro"
  }
}
```

### 3. Use in Code

Services are auto-registered. Just inject and use!

```csharp
public class MyController
{
    private readonly IAIProvider _ai;
    
    public MyController(IAIProvider ai) => _ai = ai;
    
    public async Task<string> Ask(string question)
        => await _ai.AskAsync(question);
}
```

## Common Usage Patterns

### Simple Question
```csharp
var answer = await _ai.AskAsync("What is dependency injection?");
```

### With System Message
```csharp
var answer = await _ai.AskAsync(
    "Recommend a book",
    systemMessage: "You are a helpful librarian");
```

### With Specific Model
```csharp
var answer = await _ai.AskAsync(
    "Complex question",
    model: "gpt-4o");
```

### Advanced Chat
```csharp
var request = new AIChatCompletionRequest
{
    Messages = new List<AIChatMessage>
    {
        new("system", "You are an expert programmer"),
      new("user", "Explain async/await in C#")
    },
    Temperature = 0.7,
  MaxTokens = 500
};

var response = await _ai.CreateChatCompletionAsync(request);
var answer = response.Choices[0].Message.Content;
```

### Multi-Turn Conversation
```csharp
var messages = new List<AIChatMessage>
{
    new("system", "You are a helpful assistant"),
    new("user", "What is C#?"),
    new("assistant", previousResponse),
    new("user", "Tell me more about LINQ")
};

var request = new AIChatCompletionRequest { Messages = messages };
var response = await _ai.CreateChatCompletionAsync(request);
```

### Check Token Usage
```csharp
var response = await _ai.CreateChatCompletionAsync(request);
if (response.Usage != null)
{
    Console.WriteLine($"Prompt tokens: {response.Usage.PromptTokens}");
    Console.WriteLine($"Completion tokens: {response.Usage.CompletionTokens}");
    Console.WriteLine($"Total tokens: {response.Usage.TotalTokens}");
}
```

### Error Handling
```csharp
try
{
    var answer = await _ai.AskAsync(question);
}
catch (ArgumentException)
{
    // Invalid input
}
catch (HttpRequestException)
{
// Network error
}
catch (OperationCanceledException)
{
    // Request cancelled
}
```

### With Cancellation Token
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    var answer = await _ai.AskAsync(question, cancellationToken: cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Request timed out");
}
```

## Dependency Injection

### Inject Default Provider
```csharp
public class MyService
{
    private readonly IAIProvider _ai;
    
 public MyService(IAIProvider ai)
    {
    _ai = ai;
    }
}
```

### Inject Factory (Multiple Providers)
```csharp
public class MyService
{
    private readonly IAIProviderFactory _factory;
    
    public MyService(IAIProviderFactory factory)
    {
        _factory = factory;
    }
    
    public async Task CompareProviders(string question)
    {
        var openAI = _factory.CreateProvider("OpenAI");
        var perplexity = _factory.CreateProvider("Perplexity");
     
        var result1 = await openAI.AskAsync(question);
 var result2 = await perplexity.AskAsync(question);
    }
}
```

### Inject AudiobookService
```csharp
public class AudiobookController
{
    private readonly AudiobookService _service;
    
    public AudiobookController(AudiobookService service)
    {
  _service = service;
    }
}
```

## Supported Models

### OpenAI
| Model | Speed | Cost | Use Case |
|-------|-------|------|----------|
| `gpt-4o` | Slow | High | Complex tasks, best quality |
| `gpt-4o-mini` | Fast | Low | General use (default) |
| `gpt-4-turbo` | Medium | Medium | Balanced performance |
| `gpt-3.5-turbo` | Fastest | Lowest | Simple tasks |

### Perplexity
| Model | Speed | Cost | Use Case |
|-------|-------|------|----------|
| `sonar-pro` | Medium | Medium | Research, current info (default) |
| `sonar` | Fast | Low | Quick queries |
| `sonar-reasoning` | Slow | High | Complex reasoning |

## Configuration Examples

### Switch Default Provider
```json
{
  "AI": {
    "DefaultProvider": "Perplexity"  // or "OpenAI"
  }
}
```

### Adjust Timeouts
```json
{
  "OpenAI": {
    "TimeoutSeconds": 60  // Shorter for simple queries
  },
  "Perplexity": {
    "TimeoutSeconds": 300  // Longer for research
  }
}
```

## Temperature Guide

| Temperature | Behavior | Use Case |
|-------------|----------|----------|
| 0.0 - 0.3 | Deterministic, factual | Documentation, facts, code |
| 0.4 - 0.6 | Balanced | General conversation |
| 0.7 - 0.9 | Creative, varied | Brainstorming, storytelling |
| 0.9 - 1.0 | Very creative | Poetry, fiction |

```csharp
// Factual response
var request = new AIChatCompletionRequest
{
    Messages = messages,
    Temperature = 0.1  // Very deterministic
};

// Creative response
var request = new AIChatCompletionRequest
{
    Messages = messages,
    Temperature = 0.9  // Very creative
};
```

## Cost Optimization

### Use Cheaper Models
```csharp
// For simple tasks
var answer = await _ai.AskAsync(question, model: "gpt-4o-mini");

// For complex tasks only
var answer = await _ai.AskAsync(complexQuestion, model: "gpt-4o");
```

### Limit Token Usage
```csharp
var request = new AIChatCompletionRequest
{
    Messages = messages,
    MaxTokens = 100  // Limit response length
};
```

### Cache Common Queries
```csharp
private readonly Dictionary<string, string> _cache = new();

public async Task<string> GetAnswerWithCache(string question)
{
    if (_cache.TryGetValue(question, out var cached))
 return cached;
    
    var answer = await _ai.AskAsync(question);
    _cache[question] = answer;
    return answer;
}
```

## Testing

### Mock IAIProvider
```csharp
[Fact]
public async Task TestMyService()
{
    // Arrange
    var mockAI = Substitute.For<IAIProvider>();
    mockAI.AskAsync(Arg.Any<string>())
        .Returns("Mocked response");
    
    var service = new MyService(mockAI);
    
    // Act
    var result = await service.DoSomething();
    
    // Assert
    result.Should().Contain("Mocked response");
}
```

### Test with Different Providers
```csharp
[Theory]
[InlineData("OpenAI")]
[InlineData("Perplexity")]
public async Task TestWithProvider(string providerName)
{
 var provider = _factory.CreateProvider(providerName);
    var result = await provider.AskAsync("test");
  result.Should().NotBeNullOrEmpty();
}
```

## Troubleshooting

### "Provider 'X' is not configured"
```bash
# Check environment variables are set
echo $OPENAI_API_KEY
echo $FLIBUGET_AI_APIKEY

# Or update appsettings.json with valid keys
```

### "API key cannot be null or empty"
```bash
# Set environment variable
export OPENAI_API_KEY="sk-..."

# Or remove placeholder in appsettings.json
```

### Timeout Issues
```json
{
  "OpenAI": {
    "TimeoutSeconds": 300  // Increase timeout
  }
}
```

### Build Errors
```bash
# Restore packages
dotnet restore

# Clean and rebuild
dotnet clean
dotnet build
```

## Common Scenarios

### Audiobook Search
```csharp
var answer = await _audiobookService.AskAsync(
    $"Find information about '{title}' by {author}",
    systemMessage: "You are an audiobook expert");
```

### Code Explanation
```csharp
var request = new AIChatCompletionRequest
{
    Messages = new List<AIChatMessage>
    {
   new("system", "You are a programming expert"),
        new("user", $"Explain this code:\n\n{code}")
    },
    Temperature = 0.2
};
```

### Get Structured Data
```csharp
var prompt = @"Return a JSON object with these fields:
{
  ""title"": ""book title"",
  ""author"": ""author name"",
  ""summary"": ""brief summary""
}";

var answer = await _ai.AskAsync(
    prompt,
    systemMessage: "Return only valid JSON, no additional text");
    
var data = JsonSerializer.Deserialize<BookInfo>(answer);
```

## Best Practices Checklist

- ? Use environment variables for API keys
- ? Set appropriate timeouts
- ? Monitor token usage
- ? Handle errors gracefully
- ? Use cancellation tokens for long operations
- ? Choose the right model for the task
- ? Set temperature based on use case
- ? Log API interactions
- ? Implement retry logic
- ? Cache when appropriate

## Resources

?? **Full Documentation**: `flibuget.Core\InfraServices\AI\README.md`  
?? **Examples**: `flibuget.Core\Examples\AIServiceUsageExample.cs`  
?? **Tests**: `Tests\flibuget.Core.Tests\DomainServices\AudiobookServiceTest.cs`  
?? **Summary**: `AI_INFRASTRUCTURE_SUMMARY.md`

## Common Commands

```bash
# Set environment variables (Windows)
$env:OPENAI_API_KEY = "sk-..."

# Set environment variables (Linux/Mac)
export OPENAI_API_KEY="sk-..."

# Build project
dotnet build

# Run tests
dotnet test

# Restore packages
dotnet restore
```

## Quick Tips

?? **Tip 1**: Use `gpt-4o-mini` for 90% of tasks - it's fast and cheap  
?? **Tip 2**: Lower temperature (0.1-0.3) for factual responses  
?? **Tip 3**: Set `MaxTokens` to control costs  
?? **Tip 4**: Use system messages to set behavior  
?? **Tip 5**: Always handle exceptions  
?? **Tip 6**: Monitor your usage to avoid surprises  
?? **Tip 7**: Cache frequent queries  
?? **Tip 8**: Use cancellation tokens for user-initiated requests  

---

**Ready to start?** Just inject `IAIProvider` and call `AskAsync()`! ??
