# AudiobookService - Audiobook Information Retrieval

## Overview

The `AudiobookService` now provides functionality to retrieve structured audiobook information from AI providers using natural language queries.

## Features

- ?? **Structured Information Retrieval**: Get comprehensive audiobook metadata including title, author, narrator, description, themes, and cover art links
- ?? **Automatic Response Cleaning**: Intelligently strips markdown code blocks from AI responses
- ?? **Type-Safe Responses**: Returns strongly-typed `AudiobookDescriptionDto` objects
- ? **Async/Await Support**: Fully asynchronous with cancellation token support
- ?? **Comprehensive Logging**: Detailed logging for debugging and monitoring
- ??? **Error Handling**: Robust exception handling with meaningful error messages

## API Reference

### GetAudiobookInfoFromAIAsync

Retrieves comprehensive audiobook information from an AI provider based on book title, author, and narrator.

#### Method Signature

```csharp
public async Task<AudiobookDescriptionDto> GetAudiobookInfoFromAIAsync(
    string book, 
    string author, 
    string narrator,
    CancellationToken cancellationToken = default)
```

#### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `book` | `string` | The title of the book |
| `author` | `string` | The author's name |
| `narrator` | `string` | The narrator's name (if known) |
| `cancellationToken` | `CancellationToken` | Optional cancellation token (default: `default`) |

#### Returns

Returns a `Task<AudiobookDescriptionDto>` containing:

```csharp
public class AudiobookDescriptionDto
{
    public string Title { get; set; }           // Book title
    public string Author { get; set; }          // Author name
    public string CoverLink { get; set; }       // Direct link to cover image (high resolution)
    public string Description { get; set; }     // Comprehensive plot description
    public List<string>? Themes { get; set; }   // Main themes/genres (e.g., ["Fantasy", "Adventure"])
    public string Duration { get; set; }        // Audiobook duration (e.g., "12h 45m")
    public string Narrator { get; set; }        // Narrator name
    public int? Year { get; set; }              // Publication year
    public string AgeRestriction { get; set; }  // Age restrictions (e.g., "12+")
    public string Link { get; set; }            // Link to purchase/listen
}
```

#### Exceptions

| Exception | Description |
|-----------|-------------|
| `InvalidOperationException` | Thrown when the AI response cannot be parsed or is null |
| `JsonException` | Thrown when the AI response contains invalid JSON (wrapped in `InvalidOperationException`) |
| `HttpRequestException` | Propagated from the AI provider when network requests fail |

## Usage Examples

### Example 1: Basic Usage

```csharp
var audiobookService = serviceProvider.GetRequiredService<AudiobookService>();

var info = await audiobookService.GetAudiobookInfoFromAIAsync(
    book: "The Hobbit",
    author: "J.R.R. Tolkien",
    narrator: "Andy Serkis");

Console.WriteLine($"Title: {info.Title}");
Console.WriteLine($"Author: {info.Author}");
Console.WriteLine($"Narrator: {info.Narrator}");
Console.WriteLine($"Year: {info.Year}");
Console.WriteLine($"Duration: {info.Duration}");
Console.WriteLine($"Description: {info.Description}");

if (info.Themes is { Count: > 0 })
{
    Console.WriteLine($"Themes: {string.Join(", ", info.Themes)}");
}

if (!string.IsNullOrEmpty(info.CoverLink))
{
    Console.WriteLine($"Cover: {info.CoverLink}");
}
```

### Example 2: With Error Handling

```csharp
try
{
    var info = await audiobookService.GetAudiobookInfoFromAIAsync(
        "1984",
        "George Orwell",
        "Simon Prebble");
    
    // Process the information
    ProcessAudiobookInfo(info);
}
catch (InvalidOperationException ex)
{
    _logger.LogError(ex, "Failed to retrieve audiobook information from AI");
    // Fallback to manual entry or default values
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "Network error while contacting AI service");
    // Retry logic or user notification
}
```

### Example 3: With Cancellation Token

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    var info = await audiobookService.GetAudiobookInfoFromAIAsync(
        "Dune",
        "Frank Herbert",
        "Scott Brick",
        cts.Token);
    
    Console.WriteLine($"Retrieved info for: {info.Title}");
}
catch (OperationCanceledException)
{
    _logger.LogWarning("Audiobook information request was cancelled");
}
```

### Example 4: Integration with ViewModel

```csharp
// In MainViewModel.cs
[RelayCommand]
private async Task GetInfoFromAIAsync()
{
    try
    {
        if (_audiobookService == null)
        {
            LogToConsole("AI service not available.");
            return;
        }

        LogToConsole("Fetching audiobook information from AI...");

        // Use current field values as search terms
        var searchAuthor = !string.IsNullOrWhiteSpace(Author) ? Author : "Unknown";
        var searchTitle = !string.IsNullOrWhiteSpace(Title) ? Title :
                          !string.IsNullOrWhiteSpace(Album) ? Album : "Unknown";

        // Call the service
        var dto = await _audiobookService.GetAudiobookInfoFromAIAsync(
            searchTitle, 
            searchAuthor, 
            Narrator);

        // Map AI response to UI fields
        Author = dto.Author ?? Author;
        Title = dto.Title ?? Title;
        Album = dto.Title ?? Album;
        Narrator = dto.Narrator ?? Narrator;
        Comment = dto.Description ?? Comment;
        Year = dto.Year?.ToString() ?? Year;
        Genre = dto.Themes is { Count: > 0 } 
            ? string.Join("; ", dto.Themes) 
            : "Audiobook";

        // Load cover image if available
        if (!string.IsNullOrWhiteSpace(dto.CoverLink))
        {
            await LoadCoverImageAsync(dto.CoverLink);
        }

        LogToConsole("AI data loaded successfully.");
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Error fetching data from AI");
        LogToConsole($"Error: {ex.Message}");
    }
}
```

## Implementation Details

### Markdown Stripping

The method automatically handles AI responses wrapped in markdown code blocks:

```markdown
```json
{
  "title": "The Hobbit",
  "author": "J.R.R. Tolkien"
}
```
```

Or without language specification:

```markdown
```
{
  "title": "The Hobbit",
  "author": "J.R.R. Tolkien"
}
```
```

Both formats are automatically detected and cleaned using the internal `StripMarkdownCodeBlocks` method.

### Prompt Engineering

The method uses a carefully crafted prompt that requests:
- Detailed structured JSON format
- All available metadata fields
- Informative and engaging descriptions
- Main themes and genres
- Only JSON output (no additional text)

The prompt includes a system message instructing the AI to return valid JSON responses only.

### Logging

The method provides comprehensive logging:

```
[INFO] Requesting audiobook info for: The Hobbit by J.R.R. Tolkien (narrator: Andy Serkis)
[INFO] Successfully retrieved audiobook info for: The Hobbit
```

Or in case of errors:

```
[ERROR] Failed to deserialize audiobook info response
[ERROR] Failed to parse AI response as JSON: {response}
```

## Testing

Comprehensive unit tests are provided in `AudiobookServiceTest.cs`:

### Test Coverage

? **Success Cases:**
- Valid JSON response parsing
- Markdown code block stripping (with and without language specification)
- Partial data handling
- Empty JSON object handling

? **Error Cases:**
- Invalid JSON response
- Null response
- AI provider exceptions
- Network errors

? **Behavior Tests:**
- Correct prompt construction
- System message configuration
- Cancellation token propagation

### Running Tests

```bash
dotnet test --filter "FullyQualifiedName~GetAudiobookInfoFromAIAsync"
```

## Best Practices

### 1. Always Handle Exceptions

```csharp
// ? DO: Handle potential exceptions
try
{
    var info = await service.GetAudiobookInfoFromAIAsync(book, author, narrator);
}
catch (InvalidOperationException ex)
{
    // Handle parsing errors
}
catch (HttpRequestException ex)
{
    // Handle network errors
}
```

### 2. Use Cancellation Tokens

```csharp
// ? DO: Provide cancellation tokens for long-running operations
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var info = await service.GetAudiobookInfoFromAIAsync(
    book, author, narrator, cts.Token);
```

### 3. Validate Input

```csharp
// ? DO: Provide meaningful input
var book = string.IsNullOrWhiteSpace(Title) ? "Unknown" : Title;
var author = string.IsNullOrWhiteSpace(Author) ? "Unknown" : Author;
var info = await service.GetAudiobookInfoFromAIAsync(book, author, narrator);
```

### 4. Handle Partial Data

```csharp
// ? DO: Check for null/empty values before using
if (info.Themes is { Count: > 0 })
{
    Genre = string.Join("; ", info.Themes);
}

if (!string.IsNullOrEmpty(info.CoverLink))
{
    await LoadCoverAsync(info.CoverLink);
}
```

### 5. Log Appropriately

```csharp
// ? DO: Log business events
_logger.LogInformation("Fetching audiobook info for: {Title}", title);

// ? DON'T: Log sensitive data or large payloads at INFO level
_logger.LogInformation("Full AI response: {Response}", largeResponse);
```

## Migration Guide

If you were previously calling AI methods directly from ViewModels:

### Before (Old Pattern)

```csharp
// In ViewModel
private async Task<AudiobookDescriptionDto> GetAudiobookInfoFromAIAsync(
    string book, string author, string narrator)
{
    var prompt = "..."; // Construct prompt
    var answer = await _audiobookService.AskAsync(prompt, systemMessage: "...");
    var cleanJson = StripMarkdownCodeBlocks(answer);
    return JsonSerializer.Deserialize<AudiobookDescriptionDto>(cleanJson);
}
```

### After (New Pattern)

```csharp
// In ViewModel - Just call the service
var dto = await _audiobookService.GetAudiobookInfoFromAIAsync(
    book, author, narrator);
```

**Benefits:**
- ? Less code duplication
- ? Centralized business logic
- ? Better testability
- ? Consistent error handling
- ? Improved logging

## Related Files

- **Implementation:** `flibuget.Core/DomainServices/AudiobookService.cs`
- **DTO:** `flibuget.Core/Domain/DTO/AudiobookDescriptionDto.cs`
- **Tests:** `Tests/flibuget.Core.Tests/DomainServices/AudiobookServiceTest.cs`
- **Usage:** `flibuget/ViewModels/MainViewModel.cs`

## Changelog

### Version 1.0.0 (Current)
- ? Added `GetAudiobookInfoFromAIAsync` method
- ? Added `StripMarkdownCodeBlocks` helper method
- ? Added comprehensive unit tests
- ?? Added documentation

## Support

For issues or questions:
1. Check the test cases for examples
2. Review the log output for debugging information
3. Consult the AI provider documentation for model-specific behavior
4. Raise an issue in the GitHub repository

---

**Note:** This feature requires an active AI provider connection and valid API credentials configured in the application settings.
