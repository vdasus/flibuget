using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using flibuget.Core.Domain.DTO;
using flibuget.Core.DomainServices;
using flibuget.Core.InfraServices;
using flibuget.Core.InfraServices.AudioTags;
using flibuget.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace flibuget.ViewModels;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public partial class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel>? _logger;
    private readonly IServiceProvider? _serviceProvider;
    private readonly IAudioTagService? _tagService;
    private readonly AudiobookService? _audiobookService;
    private readonly IWebService? _httpService;

    // Supported audio file extensions
    private static readonly string[] AudioExtensions = [".mp3", ".m4a", ".m4b", ".flac", ".wav", ".ogg", ".wma", ".aac"];

    #region Properties

    // File list
    [ObservableProperty]
    private ObservableCollection<AudiobookFile> audioFiles = [];

    [ObservableProperty]
    private AudiobookFile? selectedFile;

    // Tag fields - editable
    [ObservableProperty]
    private string author = string.Empty;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string album = string.Empty;

    [ObservableProperty]
    private string? year;

    // Genre can contain multiple values separated by semicolon (e.g., "Fantasy; Adventure; Humor")
    [ObservableProperty]
    private string genre = string.Empty;

    [ObservableProperty]
    private string narrator = string.Empty;

    [ObservableProperty]
    private string producer = string.Empty;

    [ObservableProperty]
    private string copyright = string.Empty;

    [ObservableProperty]
    private string publisher = string.Empty;

    [ObservableProperty]
    private string comment = string.Empty;

    [ObservableProperty]
    private string asin = string.Empty;

    [ObservableProperty]
    private Bitmap? coverImage;

    [ObservableProperty]
    private string coverImagePath = string.Empty;

    // Console/log output
    [ObservableProperty]
    private string consoleOutput = string.Empty;

    // Current project state
    [ObservableProperty]
    private string currentFolderPath = string.Empty;

    [ObservableProperty]
    private string currentProjectPath = string.Empty;

    #endregion

    // Track original tags for undo functionality
    private Dictionary<string, AudiobookTagDto> _originalTags = new();
    private Dictionary<string, AudiobookTagDto> _backupTags = new();

    // File picker delegates
    public Func<Task<string?>>? FolderPickerFunc { get; set; }
    public Func<Task<string?>>? SaveFilePickerFunc { get; set; }
    public Func<Task<string?>>? ImageFilePickerFunc { get; set; }

    // Parameterless constructor for designer support
    public MainViewModel()
    {
        // Designer mode - don't initialize services
        _logger = null;
        _serviceProvider = null;
        _tagService = null;
        _audiobookService = null;
        _httpService = null;
    }

    public MainViewModel(
        ILogger<MainViewModel> logger,
        IServiceProvider serviceProvider,
        IAudioTagService tagService,
        AudiobookService audiobookService,
        IWebService? httpService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _tagService = tagService ?? throw new ArgumentNullException(nameof(tagService));
        _audiobookService = audiobookService ?? throw new ArgumentNullException(nameof(audiobookService));
        _httpService = httpService ?? throw new ArgumentNullException(nameof(httpService));

        _logger?.LogInformation("MainViewModel initialized");
    }

    #region Project Commands

    [RelayCommand]
    private Task NewProjectAsync()
    {
        try
        {
            LogToConsole("Creating new project...");
            ClearProject();
            LogToConsole("New project created. Use 'Open Project' to load audiobook files.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating new project");
            LogToConsole($"Error: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task OpenProjectAsync()
    {
        try
        {
            // Open folder picker
            var folder = await PickFolderAsync().ConfigureAwait(false);
            if (folder == null)
            {
                LogToConsole("Folder selection cancelled.");
                return;
            }

            CurrentFolderPath = folder;
            LogToConsole($"Loading audiobook files from: {folder}");


            // Load audio files from folder
            var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
           .Where(f => AudioExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                  .OrderBy(f => f)
             .ToList();

            AudioFiles.Clear();
            foreach (var file in files)
            {
                AudioFiles.Add(new AudiobookFile(file));
            }

            LogToConsole($"Loaded {AudioFiles.Count} audio file(s).");

            // Clear tag fields
            ClearTagFields();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error opening project");
            LogToConsole($"Error opening project: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SaveProjectAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(CurrentFolderPath))
            {
                LogToConsole("No project loaded. Please open a folder first.");
                return;
            }

            var file = await PickSaveFileAsync().ConfigureAwait(false);
            if (file == null)
            {
                LogToConsole("Save cancelled.");
                return;
            }

            var project = new AudiobookProject
            {
                FolderPath = CurrentFolderPath,
                Author = Author,
                Title = Title,
                Album = Album,
                Year = int.TryParse(Year, out var y) ? y : null,
                Genre = Genre,
                Narrator = Narrator,
                Producer = Producer,
                Copyright = Copyright,
                Publisher = Publisher,
                Comment = Comment,
                ASIN = Asin,
                CoverImagePath = CoverImagePath
            };

            var json = JsonSerializer.Serialize(project, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(file, json).ConfigureAwait(false);

            CurrentProjectPath = file;
            LogToConsole($"Project saved to: {file}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving project");
            LogToConsole($"Error saving project: {ex.Message}");
        }
    }

    [RelayCommand]
    private Task CloseProjectAsync()
    {
        try
        {
            LogToConsole("Closing project...");
            ClearProject();
            LogToConsole("Project closed.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error closing project");
            LogToConsole($"Error: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Selection Commands

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var file in AudioFiles)
        {
            file.IsSelected = true;
        }
        LogToConsole($"Selected all {AudioFiles.Count} file(s).");
    }

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var file in AudioFiles)
        {
            file.IsSelected = false;
        }
        LogToConsole("Selection cleared.");
    }

    #endregion

    #region Tag Commands

    [RelayCommand]
    private async Task GetInfoFromAIAsync()
    {
        try
        {
            if (_audiobookService == null || _serviceProvider == null)
            {
                LogToConsole("AI service not available.");
                return;
            }

            LogToConsole("Fetching audiobook information from AI...");

            // Use author and title/album as search terms
            var searchAuthor = !string.IsNullOrWhiteSpace(Author) ? Author : "Unknown";
            var searchTitle = !string.IsNullOrWhiteSpace(Title) ? Title :
                      !string.IsNullOrWhiteSpace(Album) ? Album : "Unknown";

            var dto = await GetAudiobookInfoFromAIAsync(searchTitle, searchAuthor, Narrator)
                    .ConfigureAwait(true);

            // Map AI response to tag fields
            Author = dto.Author ?? Author;
            Title = dto.Title ?? Title;
            Album = dto.Title ?? Album; // AI returns book title, use it for album
            Narrator = dto.Narrator ?? Narrator;
            Comment = dto.Description ?? Comment;
            Year = dto.Year?.ToString() ?? Year;
            // Handle multiple genres/themes - join with semicolon

            Genre = dto.Themes is { Count: > 0 } ? string.Join("; ", dto.Themes) : "Audiobook";

            // Load cover image
            if (!string.IsNullOrWhiteSpace(dto.CoverLink))
            {
                try
                {
                    var imageBytes = await _httpService!.MakeGetByteArrayAsync(new Uri(dto.CoverLink)).ConfigureAwait(false);
                    using var ms = new MemoryStream(imageBytes);
                    CoverImage = new Bitmap(ms);
                    CoverImagePath = dto.CoverLink;
                    LogToConsole("Cover image loaded from AI response.");
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to load cover image");
                    LogToConsole("Warning: Could not load cover image.");
                }
            }

            LogToConsole("AI data loaded successfully.");
            LogToConsole($"Title: {dto.Title}");
            LogToConsole($"Author: {dto.Author}");
            LogToConsole($"Narrator: {dto.Narrator}");
            if (dto.Themes is { Count: > 0 })
            {
                LogToConsole($"Themes: {string.Join(", ", dto.Themes)}");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching data from AI");
            LogToConsole($"Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private Task GetInfoFromSelectedFileAsync()
    {
        try
        {
            if (_tagService == null)
            {
                LogToConsole("Tag service not available.");
                return Task.CompletedTask;
            }

            var selectedFiles = AudioFiles.Where(f => f.IsSelected).ToList();
            if (selectedFiles.Count == 0)
            {
                LogToConsole("No files selected. Please select at least one file.");
                return Task.CompletedTask;
            }

            LogToConsole($"Reading tags from {selectedFiles.Count} file(s)...");

            if (selectedFiles.Count == 1)
            {
                // Single file - load all tags
                var tags = _tagService.Read(selectedFiles[0].FilePath);
                if (tags != null)
                {
                    PopulateTagFields(tags);
                    LogToConsole($"Tags loaded from: {selectedFiles[0].FileName}");
                }
            }
            else
            {
                // Multiple files - show common values or <multiple>
                var allTags = selectedFiles
            .Select(f => _tagService.Read(f.FilePath))
              .Where(t => t != null)
                  .ToList();

                if (allTags.Count > 0)
                {
                    MergeTagsFromMultipleFiles(allTags!);
                    LogToConsole($"Tags merged from {allTags.Count} file(s). Fields with different values show '<multiple>'.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error reading tags");
            LogToConsole($"Error reading tags: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private List<string> GetTagChanges(AudiobookTagDto original, AudiobookTagDto updated)
    {
        var changes = new List<string>();

        if (!string.IsNullOrWhiteSpace(updated.Author) && updated.Author != original.Author)
            changes.Add($"Author: '{original.Author}' → '{updated.Author}'");
        if (!string.IsNullOrWhiteSpace(updated.Title) && updated.Title != original.Title)
            changes.Add($"Title: '{original.Title}' → '{updated.Title}'");
        if (!string.IsNullOrWhiteSpace(updated.Album) && updated.Album != original.Album)
            changes.Add($"Album: '{original.Album}' → '{updated.Album}'");
        if (!string.IsNullOrWhiteSpace(updated.Narrator) && updated.Narrator != original.Narrator)
            changes.Add($"Narrator: '{original.Narrator}' → '{updated.Narrator}'");
        if (!string.IsNullOrWhiteSpace(updated.Genre) && updated.Genre != original.Genre)
            changes.Add($"Genre: '{original.Genre}' → '{updated.Genre}'");
        if (!string.IsNullOrWhiteSpace(updated.Producer) && updated.Producer != original.Producer)
            changes.Add($"Producer: '{original.Producer}' → '{updated.Producer}'");
        if (!string.IsNullOrWhiteSpace(updated.Copyright) && updated.Copyright != original.Copyright)
            changes.Add($"Copyright: '{original.Copyright}' → '{updated.Copyright}'");
        if (!string.IsNullOrWhiteSpace(updated.Publisher) && updated.Publisher != original.Publisher)
            changes.Add($"Publisher: '{original.Publisher}' → '{updated.Publisher}'");
        if (!string.IsNullOrWhiteSpace(updated.Comment) && updated.Comment != original.Comment)
            changes.Add($"Comment: '{original.Comment}' → '{updated.Comment}'");
        if (!string.IsNullOrWhiteSpace(updated.ASIN) && updated.ASIN != original.ASIN)
            changes.Add($"ASIN: '{original.ASIN}' → '{updated.ASIN}'");
        if (!string.IsNullOrWhiteSpace(updated.CoverImageUrl) && updated.CoverImageUrl != original.CoverImageUrl)
            changes.Add($"CoverImage: '{original.CoverImageUrl}' → '{updated.CoverImageUrl}'");
        if (updated.Year.HasValue && updated.Year != original.Year)
            changes.Add($"Year: '{original.Year}' → '{updated.Year}'");

        return changes;
    }

    [RelayCommand]
    private Task SetTagsIntoSelectedFilesAsync()
    {
        try
        {
            if (_tagService == null)
            {
                LogToConsole("Tag service not available.");
                return Task.CompletedTask;
            }

            var selectedFiles = AudioFiles.Where(f => f.IsSelected).ToList();
            if (selectedFiles.Count == 0)
            {
                LogToConsole("No files selected. Please select files to update.");
                return Task.CompletedTask;
            }

            LogToConsole($"Writing tags to {selectedFiles.Count} file(s)...");

            // Backup original tags for undo
            _backupTags.Clear();
            foreach (var file in selectedFiles)
            {
                var originalTags = _tagService.Read(file.FilePath);
                if (originalTags != null)
                {
                    _backupTags[file.FilePath] = originalTags;
                }
            }

            // Create tag DTO from current fields (skip <multiple> placeholders and empty values)
            var tags = CreateTagDtoFromFields();

            foreach (var file in selectedFiles)
            {
                var original = _backupTags.TryGetValue(file.FilePath, out var orig) ? orig : null;
                if (original == null) continue;

                var changes = GetTagChanges(original, tags);
                LogToConsole(changes.Count > 0
                    ? $"File: {file.FileName} - Tags to be replaced:\n {string.Join("\n ", changes)}"
                    : $"File: {file.FileName} - No tags will be replaced.");
            }

            int successCount = 0;
            foreach (var file in selectedFiles)
            {
                try
                {
                    _tagService.Write(file.FilePath, tags, overwriteExisting: true);
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to write tags to {File}", file.FileName);
                    LogToConsole($"Warning: Failed to write tags to {file.FileName}");
                }
            }

            LogToConsole($"Tags written to {successCount} file(s) successfully.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error writing tags");
            LogToConsole($"Error writing tags: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task UndoAsync()
    {
        try
        {
            if (_tagService == null)
            {
                LogToConsole("Tag service not available.");
                return Task.CompletedTask;
            }

            if (_backupTags.Count == 0)
            {
                LogToConsole("No changes to undo.");
                return Task.CompletedTask;
            }

            LogToConsole($"Reverting changes to {_backupTags.Count} file(s)...");

            int successCount = 0;
            foreach (var kvp in _backupTags)
            {
                try
                {
                    _tagService.Write(kvp.Key, kvp.Value, overwriteExisting: true);
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to revert {File}", kvp.Key);
                    LogToConsole($"Warning: Failed to revert {Path.GetFileName(kvp.Key)}");
                }
            }

            _backupTags.Clear();
            LogToConsole($"Reverted changes to {successCount} file(s).");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during undo")
            ; LogToConsole($"Error during undo: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SelectCoverImageAsync()
    {
        try
        {
            var file = await PickImageFileAsync().ConfigureAwait(false);
            if (file == null)
            {
                return;
            }

            CoverImagePath = file;
            CoverImage = new Bitmap(file);
            LogToConsole($"Cover image loaded: {Path.GetFileName(file)}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading cover image");
            LogToConsole($"Error loading cover image: {ex.Message}");
        }
    }

    #endregion

    #region Helper Methods

    private void PopulateTagFields(AudiobookTagDto tags)
    {
        Author = tags.Author ?? string.Empty;
        Title = tags.Title ?? string.Empty;
        Album = tags.Album ?? string.Empty;
        Year = tags.Year?.ToString() ?? string.Empty;
        Genre = tags.Genre ?? string.Empty;
        Narrator = tags.Narrator ?? string.Empty;
        Producer = tags.Producer ?? string.Empty;
        Copyright = tags.Copyright ?? string.Empty;
        Publisher = tags.Publisher ?? string.Empty;
        Comment = tags.Comment ?? string.Empty;
        Asin = tags.ASIN ?? string.Empty;
    }

    private void MergeTagsFromMultipleFiles(List<AudiobookTagDto> allTags)
    {
        const string multiplePlaceholder = "<multiple>";

        Author = GetCommonValue(allTags.Select(t => t.Author), multiplePlaceholder);
        Title = GetCommonValue(allTags.Select(t => t.Title), multiplePlaceholder);
        Album = GetCommonValue(allTags.Select(t => t.Album), multiplePlaceholder);
        Year = GetCommonValue(allTags.Select(t => t.Year?.ToString() ?? string.Empty), multiplePlaceholder);
        Genre = GetCommonValue(allTags.Select(t => t.Genre), multiplePlaceholder);
        Narrator = GetCommonValue(allTags.Select(t => t.Narrator), multiplePlaceholder);
        Producer = GetCommonValue(allTags.Select(t => t.Producer), multiplePlaceholder);
        Copyright = GetCommonValue(allTags.Select(t => t.Copyright), multiplePlaceholder);
        Publisher = GetCommonValue(allTags.Select(t => t.Publisher), multiplePlaceholder);
        Comment = GetCommonValue(allTags.Select(t => t.Comment), multiplePlaceholder);
        Asin = GetCommonValue(allTags.Select(t => t.ASIN), multiplePlaceholder);
    }

    private string GetCommonValue(IEnumerable<string?> values, string multiplePlaceholder)
    {
        var distinctValues = values.Where(v => !string.IsNullOrEmpty(v)).Distinct().ToList();
        return distinctValues.Count == 1 ? distinctValues[0]! :
            distinctValues.Count > 1 ? multiplePlaceholder : string.Empty;
    }

    private AudiobookTagDto CreateTagDtoFromFields()
    {
        const string multiplePlaceholder = "<multiple>";

        // For fields that show <multiple>, we should keep them as null to avoid overwriting
        // Only write fields that have actual values (not empty, not <multiple>)
        return new AudiobookTagDto(
            author: Author != multiplePlaceholder && !string.IsNullOrWhiteSpace(Author) ? Author : string.Empty,
            title: Title != multiplePlaceholder && !string.IsNullOrWhiteSpace(Title) ? Title : string.Empty,
            album: Album != multiplePlaceholder && !string.IsNullOrWhiteSpace(Album) ? Album : string.Empty,
            trackNumber: null,
            year: Year != multiplePlaceholder && uint.TryParse(Year, out var y) ? y : null,
            genre: Genre != multiplePlaceholder && !string.IsNullOrWhiteSpace(Genre) ? Genre : string.Empty,
            narrator: Narrator != multiplePlaceholder && !string.IsNullOrWhiteSpace(Narrator) ? Narrator : string.Empty,
            producer: Producer != multiplePlaceholder && !string.IsNullOrWhiteSpace(Producer) ? Producer : string.Empty,
            copyright: Copyright != multiplePlaceholder && !string.IsNullOrWhiteSpace(Copyright)
                ? Copyright
                : string.Empty,
            publisher: Publisher != multiplePlaceholder && !string.IsNullOrWhiteSpace(Publisher)
                ? Publisher
                : string.Empty,
            comment: Comment != multiplePlaceholder && !string.IsNullOrWhiteSpace(Comment) ? Comment : string.Empty,
            asin: Asin != multiplePlaceholder && !string.IsNullOrWhiteSpace(Asin) ? Asin : string.Empty,
            coverImageUrl: CoverImagePath
        );
    }

    private void ClearProject()
    {
        AudioFiles.Clear();
        ClearTagFields();
        CurrentFolderPath = string.Empty;
        CurrentProjectPath = string.Empty;
        ConsoleOutput = string.Empty;
        _originalTags.Clear();
        _backupTags.Clear();
    }

    private void ClearTagFields()
    {
        Author = string.Empty;
        Title = string.Empty;
        Album = string.Empty;
        Year = string.Empty;
        Genre = string.Empty;
        Narrator = string.Empty;
        Producer = string.Empty;
        Copyright = string.Empty;
        Publisher = string.Empty;
        Comment = string.Empty;
        Asin = string.Empty;
        CoverImage = null;
        CoverImagePath = string.Empty;
    }

    private void LogToConsole(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        ConsoleOutput += $"[{timestamp}] {message}\n";
        _logger?.LogInformation(message);
    }

    // File picker methods
    private async Task<string?> PickFolderAsync()
    {
        return FolderPickerFunc != null ? await FolderPickerFunc().ConfigureAwait(false) : null;
    }

    private async Task<string?> PickSaveFileAsync()
    {
        return SaveFilePickerFunc != null ? await SaveFilePickerFunc().ConfigureAwait(false) : null;
    }

    private async Task<string?> PickImageFileAsync()
    {
        return ImageFilePickerFunc != null ? await ImageFilePickerFunc().ConfigureAwait(false) : null;
    }

    #endregion

    #region To refactor later
    private async Task<AudiobookDescriptionDto> GetAudiobookInfoFromAIAsync(string book, string author, string narrator)
    {
        var prompt = @$"Please provide a detailed structured description of the audiobook in JSON format with the following fields:

- title: book title,
- author: author name,
- cover_link: direct link to the book cover in high resolution,
- description: brief but comprehensive description of the audiobook plot, including genres and main themes,
- themes: list of main themes or genres of the book (e.g., ""fantasy"", ""adventure"", ""humor""),
- duration: audiobook duration (hours and minutes),
- narrator: name of the narrator (if known),
- year: year of publication,
- age_restriction: age restrictions (if any),
- link: link to an official or major resource where you can listen to or purchase the audiobook.

The description should be informative and engaging, reflecting the atmosphere and purpose of the work. Fields should be filled as completely as possible.

Please compose such JSON for the book ""{book}"" by {author} with narrator {narrator}.

Return ONLY the JSON object, no additional text.";

        var answer = await (_audiobookService?.AskAsync(
                prompt,
                systemMessage: "You are a helpful assistant that returns only valid JSON responses.")!)
            .ConfigureAwait(false);

        // Strip markdown code blocks if present
        var cleanJson = StripMarkdownCodeBlocks(answer);

        // Deserialize to strongly-typed object
        return JsonSerializer.Deserialize<AudiobookDescriptionDto>(cleanJson) ?? throw new InvalidOperationException("Can't get info from AI.");
    }

    /// <summary>
    /// Strips markdown code blocks from AI responses to extract pure JSON.
    /// Handles both ```json and ``` code block formats.
    /// </summary>
    /// <param name="response">The AI response that may contain markdown-wrapped JSON</param>
    /// <returns>Clean JSON string</returns>
    private static string StripMarkdownCodeBlocks(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return response;

        // Remove markdown code blocks: ```json ... ``` or ``` ... ```
        var pattern = @"^```(?:json)?\s*\n?(.*?)\n?```$";
        var match = Regex.Match(response.Trim(), pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

        return match.Success ? match.Groups[1].Value.Trim() : response.Trim();
    }
    #endregion
}
