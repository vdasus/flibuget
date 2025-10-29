using System.Text.Json.Serialization;

namespace flibuget.Core.Domain.DTO;

/// <summary>
/// Represents a chat message in the Perplexity API request
/// </summary>
public class PerplexityMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Represents the JSON schema for structured responses
/// </summary>
public class PerplexityJsonSchema
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    [JsonPropertyName("properties")]
    public Dictionary<string, object>? Properties { get; set; }

    [JsonPropertyName("required")]
    public List<string>? Required { get; set; }
}

/// <summary>
/// Represents the response format configuration
/// </summary>
public class PerplexityResponseFormat
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "json_schema";

    [JsonPropertyName("json_schema")]
    public PerplexityJsonSchemaWrapper? JsonSchema { get; set; }
}

/// <summary>
/// Wrapper for the JSON schema in the response format
/// </summary>
public class PerplexityJsonSchemaWrapper
{
    [JsonPropertyName("schema")]
    public PerplexityJsonSchema? Schema { get; set; }
}

/// <summary>
/// Represents the chat completion request to Perplexity API
/// </summary>
public class PerplexityChatCompletionRequest
{
    [JsonPropertyName("messages")]
    public List<PerplexityMessage> Messages { get; set; } = new();

    [JsonPropertyName("model")]
    public string Model { get; set; } = "sonar-pro";

    [JsonPropertyName("response_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PerplexityResponseFormat? ResponseFormat { get; set; }

    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Temperature { get; set; }

    [JsonPropertyName("max_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("top_p")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? TopP { get; set; }

    [JsonPropertyName("stream")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Stream { get; set; }
}

/// <summary>
/// Represents the usage information in the response
/// </summary>
public class PerplexityUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

/// <summary>
/// Represents a message in the completion response
/// </summary>
public class PerplexityResponseMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Represents a choice in the completion response
/// </summary>
public class PerplexityChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public PerplexityResponseMessage Message { get; set; } = new();

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; } = string.Empty;
}

/// <summary>
/// Represents the chat completion response from Perplexity API
/// </summary>
public class PerplexityChatCompletionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<PerplexityChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")]
    public PerplexityUsage? Usage { get; set; }
}
