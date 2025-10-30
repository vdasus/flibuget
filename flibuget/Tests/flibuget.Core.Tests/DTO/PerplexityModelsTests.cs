using AutoFixture.Xunit2;
using flibuget.Core.Domain.DTO;
using FluentAssertions;
using System.Text.Json;

namespace flibuget.Core.Tests.DTO;

public class PerplexityMessageTests
{
    [Theory, AutoData]
    public void Properties_Can_Be_Set_And_Retrieved(string role, string content)
  {
     // Arrange
        var message = new PerplexityMessage();

        // Act
        message.Role = role;
        message.Content = content;

   // Assert
        message.Role.Should().Be(role);
 message.Content.Should().Be(content);
    }

    [Fact]
 public void Default_Constructor_Initializes_With_Empty_Strings()
 {
        // Act
        var message = new PerplexityMessage();

// Assert
        message.Role.Should().BeEmpty();
     message.Content.Should().BeEmpty();
  }

 [Theory, AutoData]
    public void Serialization_Uses_Correct_JsonPropertyNames(string role, string content)
    {
        // Arrange
    var message = new PerplexityMessage { Role = role, Content = content };

  // Act
        var json = JsonSerializer.Serialize(message);

        // Assert
        json.Should().Contain("\"role\":");
        json.Should().Contain("\"content\":");
  }

    [Fact]
    public void Deserialization_Works_Correctly()
    {
   // Arrange
 var json = @"{""role"":""user"",""content"":""Hello""}";

     // Act
        var message = JsonSerializer.Deserialize<PerplexityMessage>(json);

        // Assert
message.Should().NotBeNull();
 message!.Role.Should().Be("user");
   message.Content.Should().Be("Hello");
    }
}

public class PerplexityJsonSchemaTests
{
    [Theory, AutoData]
    public void Properties_Can_Be_Set_And_Retrieved(
    string type,
        Dictionary<string, object> properties,
 List<string> required)
    {
   // Arrange
        var schema = new PerplexityJsonSchema();

     // Act
   schema.Type = type;
    schema.Properties = properties;
     schema.Required = required;

      // Assert
   schema.Type.Should().Be(type);
 schema.Properties.Should().BeEquivalentTo(properties);
        schema.Required.Should().BeEquivalentTo(required);
    }

    [Fact]
    public void Default_Constructor_Sets_Type_To_Object()
    {
        // Act
        var schema = new PerplexityJsonSchema();

        // Assert
 schema.Type.Should().Be("object");
   schema.Properties.Should().BeNull();
        schema.Required.Should().BeNull();
}

  [Fact]
    public void Serialization_Uses_Correct_JsonPropertyNames()
    {
 // Arrange
   var schema = new PerplexityJsonSchema
 {
    Type = "object",
  Properties = new Dictionary<string, object> { { "name", "string" } },
     Required = new List<string> { "name" }
};

   // Act
        var json = JsonSerializer.Serialize(schema);

        // Assert
      json.Should().Contain("\"type\":");
 json.Should().Contain("\"properties\":");
      json.Should().Contain("\"required\":");
    }
}

public class PerplexityResponseFormatTests
{
    [Theory, AutoData]
    public void Properties_Can_Be_Set_And_Retrieved(
   string type,
 PerplexityJsonSchemaWrapper jsonSchema)
    {
        // Arrange
     var format = new PerplexityResponseFormat();

    // Act
 format.Type = type;
        format.JsonSchema = jsonSchema;

        // Assert
        format.Type.Should().Be(type);
      format.JsonSchema.Should().Be(jsonSchema);
    }

    [Fact]
    public void Default_Constructor_Sets_Type_To_JsonSchema()
    {
        // Act
     var format = new PerplexityResponseFormat();

        // Assert
   format.Type.Should().Be("json_schema");
    format.JsonSchema.Should().BeNull();
    }

    [Fact]
    public void Serialization_Uses_Correct_JsonPropertyNames()
  {
        // Arrange
        var format = new PerplexityResponseFormat
        {
            Type = "json_schema",
   JsonSchema = new PerplexityJsonSchemaWrapper
       {
    Schema = new PerplexityJsonSchema { Type = "object" }
}
        };

   // Act
        var json = JsonSerializer.Serialize(format);

     // Assert
        json.Should().Contain("\"type\":");
        json.Should().Contain("\"json_schema\":");
    }
}

public class PerplexityJsonSchemaWrapperTests
{
    [Theory, AutoData]
    public void Schema_Property_Can_Be_Set_And_Retrieved(PerplexityJsonSchema schema)
    {
        // Arrange
 var wrapper = new PerplexityJsonSchemaWrapper();

        // Act
        wrapper.Schema = schema;

        // Assert
        wrapper.Schema.Should().Be(schema);
    }

    [Fact]
    public void Default_Constructor_Initializes_Schema_As_Null()
    {
    // Act
        var wrapper = new PerplexityJsonSchemaWrapper();

        // Assert
   wrapper.Schema.Should().BeNull();
    }

    [Fact]
 public void Serialization_Uses_Correct_JsonPropertyName()
    {
        // Arrange
        var wrapper = new PerplexityJsonSchemaWrapper
        {
    Schema = new PerplexityJsonSchema { Type = "object" }
        };

        // Act
        var json = JsonSerializer.Serialize(wrapper);

   // Assert
   json.Should().Contain("\"schema\":");
    }
}

public class PerplexityChatCompletionRequestTests
{
    [Theory, AutoData]
 public void Properties_Can_Be_Set_And_Retrieved(
     List<PerplexityMessage> messages,
      string model,
   PerplexityResponseFormat responseFormat,
      double temperature,
   int maxTokens,
     double topP,
    bool stream)
    {
     // Arrange
        var request = new PerplexityChatCompletionRequest();

 // Act
        request.Messages = messages;
   request.Model = model;
   request.ResponseFormat = responseFormat;
        request.Temperature = temperature;
    request.MaxTokens = maxTokens;
        request.TopP = topP;
        request.Stream = stream;

        // Assert
  request.Messages.Should().BeEquivalentTo(messages);
        request.Model.Should().Be(model);
   request.ResponseFormat.Should().Be(responseFormat);
   request.Temperature.Should().Be(temperature);
   request.MaxTokens.Should().Be(maxTokens);
        request.TopP.Should().Be(topP);
        request.Stream.Should().Be(stream);
    }

    [Fact]
    public void Default_Constructor_Initializes_With_Default_Values()
    {
        // Act
        var request = new PerplexityChatCompletionRequest();

   // Assert
request.Messages.Should().NotBeNull().And.BeEmpty();
   request.Model.Should().Be("sonar-pro");
        request.ResponseFormat.Should().BeNull();
request.Temperature.Should().BeNull();
 request.MaxTokens.Should().BeNull();
        request.TopP.Should().BeNull();
 request.Stream.Should().BeNull();
    }

 [Fact]
    public void Serialization_Uses_Correct_JsonPropertyNames()
    {
  // Arrange
     var request = new PerplexityChatCompletionRequest
        {
    Messages = new List<PerplexityMessage>
      {
       new() { Role = "user", Content = "Hello" }
 },
   Model = "sonar-pro",
       Temperature = 0.7,
MaxTokens = 1000,
 TopP = 0.9,
   Stream = false
        };

    // Act
        var json = JsonSerializer.Serialize(request);

// Assert
        json.Should().Contain("\"messages\":");
        json.Should().Contain("\"model\":");
      json.Should().Contain("\"temperature\":");
        json.Should().Contain("\"max_tokens\":");
        json.Should().Contain("\"top_p\":");
   json.Should().Contain("\"stream\":");
    }

    [Fact]
    public void Serialization_Omits_Null_Optional_Properties()
    {
     // Arrange
        var request = new PerplexityChatCompletionRequest
        {
        Messages = new List<PerplexityMessage>
 {
     new() { Role = "user", Content = "Hello" }
            }
};

        // Act
        var json = JsonSerializer.Serialize(request);

     // Assert
        json.Should().NotContain("\"response_format\":");
        json.Should().NotContain("\"temperature\":");
        json.Should().NotContain("\"max_tokens\":");
   json.Should().NotContain("\"top_p\":");
   json.Should().NotContain("\"stream\":");
    }

    [Fact]
    public void Deserialization_Works_Correctly()
    {
    // Arrange
   var json = @"{
  ""messages"": [{""role"":""user"",""content"":""Hello""}],
  ""model"": ""sonar-pro"",
  ""temperature"": 0.7,
  ""max_tokens"": 1000
}";

    // Act
   var request = JsonSerializer.Deserialize<PerplexityChatCompletionRequest>(json);

  // Assert
     request.Should().NotBeNull();
      request!.Messages.Should().HaveCount(1);
request.Model.Should().Be("sonar-pro");
    request.Temperature.Should().Be(0.7);
     request.MaxTokens.Should().Be(1000);
    }

    [Fact]
    public void Serialization_Roundtrip_Preserves_All_Data()
    {
      // Arrange
        var original = new PerplexityChatCompletionRequest
     {
          Messages = new List<PerplexityMessage>
   {
          new() { Role = "system", Content = "You are helpful" },
       new() { Role = "user", Content = "Hello" }
       },
Model = "sonar-pro",
       Temperature = 0.8,
            MaxTokens = 2000,
  TopP = 0.95,
     Stream = true
};

 // Act
 var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<PerplexityChatCompletionRequest>(json);

        // Assert
   deserialized.Should().BeEquivalentTo(original);
    }
}

public class PerplexityUsageTests
{
    [Theory, AutoData]
public void Properties_Can_Be_Set_And_Retrieved(
        int promptTokens,
  int completionTokens,
   int totalTokens)
    {
     // Arrange
        var usage = new PerplexityUsage();

        // Act
        usage.PromptTokens = promptTokens;
     usage.CompletionTokens = completionTokens;
        usage.TotalTokens = totalTokens;

 // Assert
   usage.PromptTokens.Should().Be(promptTokens);
   usage.CompletionTokens.Should().Be(completionTokens);
 usage.TotalTokens.Should().Be(totalTokens);
  }

    [Fact]
    public void Default_Constructor_Initializes_With_Zero_Values()
    {
        // Act
     var usage = new PerplexityUsage();

    // Assert
usage.PromptTokens.Should().Be(0);
        usage.CompletionTokens.Should().Be(0);
  usage.TotalTokens.Should().Be(0);
    }

    [Fact]
    public void Serialization_Uses_Correct_JsonPropertyNames()
    {
        // Arrange
  var usage = new PerplexityUsage
        {
     PromptTokens = 10,
            CompletionTokens = 20,
   TotalTokens = 30
        };

        // Act
        var json = JsonSerializer.Serialize(usage);

 // Assert
        json.Should().Contain("\"prompt_tokens\":");
 json.Should().Contain("\"completion_tokens\":");
        json.Should().Contain("\"total_tokens\":");
    }

    [Fact]
    public void Deserialization_Works_Correctly()
    {
   // Arrange
  var json = @"{""prompt_tokens"":15,""completion_tokens"":25,""total_tokens"":40}";

    // Act
        var usage = JsonSerializer.Deserialize<PerplexityUsage>(json);

   // Assert
  usage.Should().NotBeNull();
   usage!.PromptTokens.Should().Be(15);
    usage.CompletionTokens.Should().Be(25);
     usage.TotalTokens.Should().Be(40);
    }
}

public class PerplexityResponseMessageTests
{
    [Theory, AutoData]
    public void Properties_Can_Be_Set_And_Retrieved(string role, string content)
    {
   // Arrange
        var message = new PerplexityResponseMessage();

  // Act
        message.Role = role;
        message.Content = content;

        // Assert
    message.Role.Should().Be(role);
        message.Content.Should().Be(content);
  }

    [Fact]
    public void Default_Constructor_Initializes_With_Empty_Strings()
  {
        // Act
 var message = new PerplexityResponseMessage();

   // Assert
        message.Role.Should().BeEmpty();
        message.Content.Should().BeEmpty();
    }

    [Fact]
    public void Serialization_Uses_Correct_JsonPropertyNames()
    {
        // Arrange
    var message = new PerplexityResponseMessage
        {
         Role = "assistant",
        Content = "Response text"
        };

     // Act
        var json = JsonSerializer.Serialize(message);

        // Assert
        json.Should().Contain("\"role\":");
        json.Should().Contain("\"content\":");
    }
}

public class PerplexityChoiceTests
{
    [Theory, AutoData]
public void Properties_Can_Be_Set_And_Retrieved(
        int index,
   PerplexityResponseMessage message,
     string finishReason)
    {
        // Arrange
        var choice = new PerplexityChoice();

        // Act
   choice.Index = index;
      choice.Message = message;
        choice.FinishReason = finishReason;

        // Assert
     choice.Index.Should().Be(index);
   choice.Message.Should().Be(message);
 choice.FinishReason.Should().Be(finishReason);
    }

    [Fact]
    public void Default_Constructor_Initializes_With_Default_Values()
    {
// Act
        var choice = new PerplexityChoice();

        // Assert
        choice.Index.Should().Be(0);
        choice.Message.Should().NotBeNull();
        choice.FinishReason.Should().BeEmpty();
    }

[Fact]
    public void Serialization_Uses_Correct_JsonPropertyNames()
    {
        // Arrange
        var choice = new PerplexityChoice
        {
 Index = 0,
     Message = new PerplexityResponseMessage
            {
Role = "assistant",
          Content = "Hello"
    },
            FinishReason = "stop"
        };

        // Act
        var json = JsonSerializer.Serialize(choice);

     // Assert
        json.Should().Contain("\"index\":");
     json.Should().Contain("\"message\":");
        json.Should().Contain("\"finish_reason\":");
    }

  [Fact]
    public void Deserialization_Works_Correctly()
    {
        // Arrange
        var json = @"{
  ""index"": 0,
  ""message"": {""role"":""assistant"",""content"":""Test""},
  ""finish_reason"": ""stop""
}";

        // Act
  var choice = JsonSerializer.Deserialize<PerplexityChoice>(json);

     // Assert
        choice.Should().NotBeNull();
        choice!.Index.Should().Be(0);
      choice.Message.Content.Should().Be("Test");
        choice.FinishReason.Should().Be("stop");
    }
}

public class PerplexityChatCompletionResponseTests
{
    [Theory, AutoData]
  public void Properties_Can_Be_Set_And_Retrieved(
        string id,
        string objectType,
 long created,
        string model,
        List<PerplexityChoice> choices,
   PerplexityUsage usage)
{
     // Arrange
  var response = new PerplexityChatCompletionResponse();

      // Act
        response.Id = id;
        response.Object = objectType;
        response.Created = created;
   response.Model = model;
        response.Choices = choices;
        response.Usage = usage;

        // Assert
 response.Id.Should().Be(id);
        response.Object.Should().Be(objectType);
  response.Created.Should().Be(created);
        response.Model.Should().Be(model);
        response.Choices.Should().BeEquivalentTo(choices);
 response.Usage.Should().Be(usage);
    }

    [Fact]
    public void Default_Constructor_Initializes_With_Default_Values()
    {
     // Act
        var response = new PerplexityChatCompletionResponse();

        // Assert
    response.Id.Should().BeEmpty();
        response.Object.Should().BeEmpty();
        response.Created.Should().Be(0);
     response.Model.Should().BeEmpty();
     response.Choices.Should().NotBeNull().And.BeEmpty();
 response.Usage.Should().BeNull();
    }

    [Fact]
    public void Serialization_Uses_Correct_JsonPropertyNames()
    {
        // Arrange
        var response = new PerplexityChatCompletionResponse
        {
    Id = "test-id",
         Object = "chat.completion",
   Created = 1234567890,
     Model = "sonar-pro",
        Choices = new List<PerplexityChoice>
            {
    new()
{
             Index = 0,
 Message = new PerplexityResponseMessage
     {
  Role = "assistant",
         Content = "Hello"
      },
    FinishReason = "stop"
  }
         },
       Usage = new PerplexityUsage
     {
        PromptTokens = 10,
      CompletionTokens = 20,
             TotalTokens = 30
          }
      };

        // Act
   var json = JsonSerializer.Serialize(response);

      // Assert
        json.Should().Contain("\"id\":");
        json.Should().Contain("\"object\":");
        json.Should().Contain("\"created\":");
      json.Should().Contain("\"model\":");
   json.Should().Contain("\"choices\":");
  json.Should().Contain("\"usage\":");
    }

    [Fact]
    public void Deserialization_Works_Correctly()
 {
        // Arrange
        var json = @"{
  ""id"": ""test-123"",
  ""object"": ""chat.completion"",
  ""created"": 1234567890,
  ""model"": ""sonar-pro"",
  ""choices"": [
    {
      ""index"": 0,
   ""message"": {
        ""role"": ""assistant"",
        ""content"": ""Response text""
      },
      ""finish_reason"": ""stop""
    }
  ],
  ""usage"": {
    ""prompt_tokens"": 15,
  ""completion_tokens"": 25,
    ""total_tokens"": 40
  }
}";

 // Act
   var response = JsonSerializer.Deserialize<PerplexityChatCompletionResponse>(json);

      // Assert
   response.Should().NotBeNull();
   response!.Id.Should().Be("test-123");
    response.Object.Should().Be("chat.completion");
response.Created.Should().Be(1234567890);
 response.Model.Should().Be("sonar-pro");
  response.Choices.Should().HaveCount(1);
response.Usage.Should().NotBeNull();
        response.Usage!.TotalTokens.Should().Be(40);
    }

    [Fact]
    public void Serialization_Roundtrip_Preserves_All_Data()
    {
        // Arrange
        var original = new PerplexityChatCompletionResponse
        {
            Id = "completion-abc123",
            Object = "chat.completion",
          Created = 1699999999,
     Model = "sonar-pro",
  Choices = new List<PerplexityChoice>
        {
             new()
                {
        Index = 0,
  Message = new PerplexityResponseMessage
            {
        Role = "assistant",
  Content = "This is a test response"
   },
       FinishReason = "stop"
       }
    },
       Usage = new PerplexityUsage
    {
    PromptTokens = 50,
      CompletionTokens = 100,
                TotalTokens = 150
   }
   };

   // Act
     var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<PerplexityChatCompletionResponse>(json);

        // Assert
        deserialized.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void Deserialization_Handles_Null_Usage()
    {
 // Arrange
  var json = @"{
  ""id"": ""test-123"",
  ""object"": ""chat.completion"",
  ""created"": 1234567890,
  ""model"": ""sonar-pro"",
  ""choices"": []
}";

   // Act
   var response = JsonSerializer.Deserialize<PerplexityChatCompletionResponse>(json);

 // Assert
 response.Should().NotBeNull();
  response!.Usage.Should().BeNull();
    }
}
