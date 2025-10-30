using System.Text.Json.Serialization;

namespace flibuget.Core.InfraServices.AI;

/// <summary>
/// Represents a chat message
/// </summary>
public class AIChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

  public AIChatMessage() { }

    public AIChatMessage(string role, string content)
    {
        Role = role;
        Content = content;
    }
}

/// <summary>
/// Represents a chat completion request
/// </summary>
public class AIChatCompletionRequest
{
    public List<AIChatMessage> Messages { get; set; } = new();
    public string? Model { get; set; }
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public double? TopP { get; set; }
    public bool? Stream { get; set; }
    public object? ResponseFormat { get; set; }
}

/// <summary>
/// Represents the usage information in the response
/// </summary>
public class AIUsage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}

/// <summary>
/// Represents a choice in the completion response
/// </summary>
public class AIChoice
{
    public int Index { get; set; }
    public AIChatMessage Message { get; set; } = new();
public string FinishReason { get; set; } = string.Empty;
}

/// <summary>
/// Represents the chat completion response
/// </summary>
public class AIChatCompletionResponse
{
    public string Id { get; set; } = string.Empty;
    public string Object { get; set; } = string.Empty;
    public long Created { get; set; }
    public string Model { get; set; } = string.Empty;
    public List<AIChoice> Choices { get; set; } = new();
    public AIUsage? Usage { get; set; }
}

/// <summary>
/// Configuration for an AI provider
/// </summary>
public class AIProviderConfig
{
    public string ProviderName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string? BaseUrl { get; set; }
    public string? DefaultModel { get; set; }
    public int TimeoutSeconds { get; set; } = 240;
}
