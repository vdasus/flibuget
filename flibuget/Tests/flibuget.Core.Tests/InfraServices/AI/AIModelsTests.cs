using AutoFixture.Xunit2;
using flibuget.Core.InfraServices.AI;
using FluentAssertions;
using System.Text.Json;

namespace flibuget.Core.Tests.InfraServices.AI;

public class AIChatMessageTests
{
    [Theory, AutoData]
public void Properties_Can_Be_Set_And_Retrieved(string role, string content)
  {
        // Arrange
        var message = new AIChatMessage();

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
     var message = new AIChatMessage();

        // Assert
        message.Role.Should().BeEmpty();
        message.Content.Should().BeEmpty();
    }

    [Theory, AutoData]
    public void Parameterized_Constructor_Sets_Properties(string role, string content)
    {
        // Act
     var message = new AIChatMessage(role, content);

        // Assert
        message.Role.Should().Be(role);
        message.Content.Should().Be(content);
    }

    [Theory, AutoData]
    public void Serialization_Uses_Correct_JsonPropertyNames(string role, string content)
    {
        // Arrange
        var message = new AIChatMessage(role, content);

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
      var message = JsonSerializer.Deserialize<AIChatMessage>(json);

        // Assert
        message.Should().NotBeNull();
   message!.Role.Should().Be("user");
        message.Content.Should().Be("Hello");
    }

    [Fact]
    public void Serialization_Roundtrip_Preserves_All_Data()
    {
        // Arrange
        var original = new AIChatMessage("assistant", "Test message");

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<AIChatMessage>(json);

   // Assert
        deserialized.Should().BeEquivalentTo(original);
  }
}

public class AIChatCompletionRequestTests
{
    [Theory, AutoData]
    public void Properties_Can_Be_Set_And_Retrieved(
        List<AIChatMessage> messages,
        string model,
        double temperature,
   int maxTokens,
        double topP,
        bool stream,
      object responseFormat)
    {
  // Arrange
        var request = new AIChatCompletionRequest();

        // Act
      request.Messages = messages;
   request.Model = model;
  request.Temperature = temperature;
request.MaxTokens = maxTokens;
   request.TopP = topP;
 request.Stream = stream;
        request.ResponseFormat = responseFormat;

   // Assert
        request.Messages.Should().BeEquivalentTo(messages);
 request.Model.Should().Be(model);
      request.Temperature.Should().Be(temperature);
        request.MaxTokens.Should().Be(maxTokens);
        request.TopP.Should().Be(topP);
        request.Stream.Should().Be(stream);
        request.ResponseFormat.Should().Be(responseFormat);
    }

    [Fact]
public void Default_Constructor_Initializes_With_Default_Values()
    {
        // Act
        var request = new AIChatCompletionRequest();

        // Assert
        request.Messages.Should().NotBeNull().And.BeEmpty();
        request.Model.Should().BeNull();
        request.Temperature.Should().BeNull();
  request.MaxTokens.Should().BeNull();
        request.TopP.Should().BeNull();
   request.Stream.Should().BeNull();
        request.ResponseFormat.Should().BeNull();
    }

    [Theory, AutoData]
    public void Messages_List_Can_Be_Modified(AIChatMessage message1, AIChatMessage message2)
    {
        // Arrange
        var request = new AIChatCompletionRequest();

        // Act
        request.Messages.Add(message1);
        request.Messages.Add(message2);

        // Assert
        request.Messages.Should().HaveCount(2);
        request.Messages.Should().Contain(message1);
        request.Messages.Should().Contain(message2);
    }
}

public class AIUsageTests
{
    [Theory, AutoData]
    public void Properties_Can_Be_Set_And_Retrieved(
        int promptTokens,
        int completionTokens,
        int totalTokens)
    {
      // Arrange
        var usage = new AIUsage();

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
        var usage = new AIUsage();

     // Assert
        usage.PromptTokens.Should().Be(0);
    usage.CompletionTokens.Should().Be(0);
    usage.TotalTokens.Should().Be(0);
    }
}

public class AIChoiceTests
{
    [Theory, AutoData]
    public void Properties_Can_Be_Set_And_Retrieved(
        int index,
      AIChatMessage message,
        string finishReason)
    {
   // Arrange
        var choice = new AIChoice();

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
 var choice = new AIChoice();

        // Assert
 choice.Index.Should().Be(0);
        choice.Message.Should().NotBeNull();
        choice.FinishReason.Should().BeEmpty();
    }
}

public class AIChatCompletionResponseTests
{
    [Theory, AutoData]
    public void Properties_Can_Be_Set_And_Retrieved(
        string id,
      string obj,
        long created,
        string model,
        List<AIChoice> choices,
     AIUsage usage)
 {
        // Arrange
        var response = new AIChatCompletionResponse();

      // Act
        response.Id = id;
        response.Object = obj;
        response.Created = created;
   response.Model = model;
   response.Choices = choices;
  response.Usage = usage;

  // Assert
        response.Id.Should().Be(id);
 response.Object.Should().Be(obj);
        response.Created.Should().Be(created);
        response.Model.Should().Be(model);
        response.Choices.Should().BeEquivalentTo(choices);
        response.Usage.Should().Be(usage);
    }

    [Fact]
    public void Default_Constructor_Initializes_With_Default_Values()
    {
        // Act
     var response = new AIChatCompletionResponse();

        // Assert
        response.Id.Should().BeEmpty();
        response.Object.Should().BeEmpty();
        response.Created.Should().Be(0);
  response.Model.Should().BeEmpty();
  response.Choices.Should().NotBeNull().And.BeEmpty();
     response.Usage.Should().BeNull();
    }

    [Fact]
    public void Choices_List_Can_Be_Modified()
    {
     // Arrange
        var response = new AIChatCompletionResponse();
  var choice = new AIChoice
        {
   Index = 0,
       Message = new AIChatMessage("assistant", "Test"),
    FinishReason = "stop"
        };

        // Act
        response.Choices.Add(choice);

        // Assert
        response.Choices.Should().HaveCount(1);
      response.Choices.Should().Contain(choice);
  }
}

public class AIProviderConfigTests
{
    [Theory, AutoData]
    public void Properties_Can_Be_Set_And_Retrieved(
        string providerName,
  string apiKey,
        string baseUrl,
      string defaultModel,
        int timeoutSeconds)
    {
    // Arrange
        var config = new AIProviderConfig();

    // Act
     config.ProviderName = providerName;
config.ApiKey = apiKey;
        config.BaseUrl = baseUrl;
     config.DefaultModel = defaultModel;
        config.TimeoutSeconds = timeoutSeconds;

        // Assert
        config.ProviderName.Should().Be(providerName);
        config.ApiKey.Should().Be(apiKey);
        config.BaseUrl.Should().Be(baseUrl);
        config.DefaultModel.Should().Be(defaultModel);
        config.TimeoutSeconds.Should().Be(timeoutSeconds);
    }

 [Fact]
    public void Default_Constructor_Initializes_With_Default_Values()
    {
 // Act
        var config = new AIProviderConfig();

    // Assert
      config.ProviderName.Should().BeEmpty();
     config.ApiKey.Should().BeEmpty();
        config.BaseUrl.Should().BeNull();
        config.DefaultModel.Should().BeNull();
        config.TimeoutSeconds.Should().Be(240);
    }

    [Fact]
    public void TimeoutSeconds_Has_Default_Value_Of_240()
    {
        // Act
        var config = new AIProviderConfig();

        // Assert
        config.TimeoutSeconds.Should().Be(240);
    }

    [Theory, AutoData]
    public void Config_Can_Be_Created_With_Object_Initializer(
        string providerName,
      string apiKey,
        string baseUrl)
    {
        // Act
        var config = new AIProviderConfig
        {
            ProviderName = providerName,
   ApiKey = apiKey,
         BaseUrl = baseUrl,
       DefaultModel = "gpt-4",
  TimeoutSeconds = 300
     };

        // Assert
        config.ProviderName.Should().Be(providerName);
        config.ApiKey.Should().Be(apiKey);
        config.BaseUrl.Should().Be(baseUrl);
   config.DefaultModel.Should().Be("gpt-4");
     config.TimeoutSeconds.Should().Be(300);
    }
}
