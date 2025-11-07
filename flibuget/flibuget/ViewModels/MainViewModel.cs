using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using flibuget.Core.Domain.Attributes;
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
using System.Reflection;
using System.Text.Json;
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

    // Dynamic tag fields - manually implemented to avoid source generator timing issues
    private ObservableCollection<TagFieldItem> _tagFields = [];
    public ObservableCollection<TagFieldItem> TagFields
    {
        get => _tagFields;
        set => SetProperty(ref _tagFields, value);
    }

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

        InitializeTagFields();
        _logger?.LogInformation("MainViewModel initialized");
    }

    /// <summary>
    /// Initialize tag fields from AudiobookTagDto properties using reflection and TagDisplay attributes.
    /// </summary>
    private void InitializeTagFields()
    {
        var properties = typeof(AudiobookTagDto).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var tagFieldsList = new List<TagFieldItem>();

        foreach (var prop in properties)
        {
            var attr = prop.GetCustomAttribute<TagDisplayAttribute>();
            
            // Skip ignored properties
            if (attr?.Ignore == true)
                continue;

            var displayName = attr?.DisplayName ?? AddSpacesToPropertyName(prop.Name);
            var watermark = attr?.Watermark ?? string.Empty;
            var order = attr?.Order ?? int.MaxValue;
            var isMultiLine = attr?.IsMultiLine ?? false;

            tagFieldsList.Add(new TagFieldItem
            {
                PropertyName = prop.Name,
                DisplayName = displayName,
                Value = string.Empty,
                Watermark = watermark,
                Order = order,
                IsMultiLine = isMultiLine
            });
        }

        // Sort by order, then by display name
        TagFields = new ObservableCollection<TagFieldItem>(
            tagFieldsList.OrderBy(f => f.Order).ThenBy(f => f.DisplayName)
        );
    }

    /// <summary>
    /// Convert property name to display name by adding spaces before capitals.
    /// Example: "TrackNumber" -> "Track Number"
    /// </summary>
    private static string AddSpacesToPropertyName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var result = new System.Text.StringBuilder();
        result.Append(name[0]);

        for (int i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && i > 0 && !char.IsUpper(name[i - 1]))
            {
                result.Append(' ');
            }
            result.Append(name[i]);
        }

        return result.ToString();
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

            // Helper to get field value
            string GetFieldValue(string propertyName)
            {
                return TagFields.FirstOrDefault(f => f.PropertyName == propertyName)?.Value ?? string.Empty;
            }

            var project = new AudiobookProject
            {
                FolderPath = CurrentFolderPath,
                Author = GetFieldValue(nameof(AudiobookTagDto.Author)),
                Title = GetFieldValue(nameof(AudiobookTagDto.Title)),
                Album = GetFieldValue(nameof(AudiobookTagDto.Album)),
                Year = int.TryParse(GetFieldValue(nameof(AudiobookTagDto.Year)), out var y) ? y : null,
                Genre = GetFieldValue(nameof(AudiobookTagDto.Genre)),
                Narrator = GetFieldValue(nameof(AudiobookTagDto.Narrator)),
                Producer = GetFieldValue(nameof(AudiobookTagDto.Producer)),
                Copyright = GetFieldValue(nameof(AudiobookTagDto.Copyright)),
                Publisher = GetFieldValue(nameof(AudiobookTagDto.Publisher)),
                Comment = GetFieldValue(nameof(AudiobookTagDto.Comment)),
                ASIN = GetFieldValue(nameof(AudiobookTagDto.ASIN)),
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
            var authorField = TagFields.FirstOrDefault(f => f.PropertyName == nameof(AudiobookTagDto.Author));
            var titleField = TagFields.FirstOrDefault(f => f.PropertyName == nameof(AudiobookTagDto.Title));
            var albumField = TagFields.FirstOrDefault(f => f.PropertyName == nameof(AudiobookTagDto.Album));
            var narratorField = TagFields.FirstOrDefault(f => f.PropertyName == nameof(AudiobookTagDto.Narrator));

            var searchAuthor = !string.IsNullOrWhiteSpace(authorField?.Value) ? authorField.Value : "Unknown";
            var searchTitle = !string.IsNullOrWhiteSpace(titleField?.Value) ? titleField.Value :
                      !string.IsNullOrWhiteSpace(albumField?.Value) ? albumField.Value : "Unknown";
            var searchNarrator = narratorField?.Value ?? string.Empty;

            var dto = await _audiobookService.GetAudiobookInfoFromAIAsync(searchTitle, searchAuthor, searchNarrator)
                    .ConfigureAwait(true);

            // Map AI response to tag fields
            UpdateFieldValue(nameof(AudiobookTagDto.Author), dto.Author);
            UpdateFieldValue(nameof(AudiobookTagDto.Title), dto.Title);
            UpdateFieldValue(nameof(AudiobookTagDto.Album), dto.Title); // AI returns book title, use it for album
            UpdateFieldValue(nameof(AudiobookTagDto.Narrator), dto.Narrator);
            UpdateFieldValue(nameof(AudiobookTagDto.Comment), dto.Description);
            UpdateFieldValue(nameof(AudiobookTagDto.Year), dto.Year?.ToString());
            
            // Handle multiple genres/themes - join with semicolon
            var genreValue = dto.Themes is { Count: > 0 } ? string.Join("; ", dto.Themes) : "Audiobook";
            UpdateFieldValue(nameof(AudiobookTagDto.Genre), genreValue);

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

    /// <summary>
    /// Helper to update a tag field value, only if the new value is not null/empty.
    /// </summary>
    private void UpdateFieldValue(string propertyName, string? newValue)
    {
        if (string.IsNullOrWhiteSpace(newValue)) return;
        
        var field = TagFields.FirstOrDefault(f => f.PropertyName == propertyName);
        if (field != null)
        {
            field.Value = newValue;
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
        foreach (var field in TagFields)
        {
            var prop = typeof(AudiobookTagDto).GetProperty(field.PropertyName);
            if (prop == null) continue;

            var value = prop.GetValue(tags);
            field.Value = value?.ToString() ?? string.Empty;
        }

        // Load cover image if present
        if (!string.IsNullOrEmpty(tags.CoverImageUrl) && File.Exists(tags.CoverImageUrl))
        {
            try
            {
                CoverImage = new Bitmap(tags.CoverImageUrl);
                CoverImagePath = tags.CoverImageUrl;
            }
            catch { CoverImage = null; CoverImagePath = string.Empty; }
        }
        else
        {
            CoverImage = null;
            CoverImagePath = string.Empty;
        }
    }

    private void MergeTagsFromMultipleFiles(List<AudiobookTagDto> allTags)
    {
        const string multiplePlaceholder = "<multiple>";

        foreach (var field in TagFields)
        {
            var prop = typeof(AudiobookTagDto).GetProperty(field.PropertyName);
            if (prop == null) continue;

            var values = allTags
                .Select(dto => prop.GetValue(dto)?.ToString() ?? string.Empty)
                .Where(v => !string.IsNullOrEmpty(v))
                .Distinct()
                .ToList();

            field.Value = values.Count == 1 ? values[0] :
                         values.Count > 1 ? multiplePlaceholder : string.Empty;
        }
    }

    private AudiobookTagDto CreateTagDtoFromFields()
    {
        const string multiplePlaceholder = "<multiple>";

        // Helper to get field value or empty string
        string GetFieldValue(string propertyName)
        {
            var field = TagFields.FirstOrDefault(f => f.PropertyName == propertyName);
            if (field == null) return string.Empty;
            
            var value = field.Value;
            return value != multiplePlaceholder && !string.IsNullOrWhiteSpace(value) ? value : string.Empty;
        }

        // Helper to get nullable uint for Year
        uint? GetYearValue()
        {
            var field = TagFields.FirstOrDefault(f => f.PropertyName == nameof(AudiobookTagDto.Year));
            if (field == null || field.Value == multiplePlaceholder) return null;
            return uint.TryParse(field.Value, out var y) ? y : null;
        }

        // Helper to get nullable int for TrackNumber
        int? GetTrackNumberValue()
        {
            var field = TagFields.FirstOrDefault(f => f.PropertyName == nameof(AudiobookTagDto.TrackNumber));
            if (field == null || field.Value == multiplePlaceholder) return null;
            return int.TryParse(field.Value, out var t) ? t : null;
        }

        return new AudiobookTagDto(
            author: GetFieldValue(nameof(AudiobookTagDto.Author)),
            title: GetFieldValue(nameof(AudiobookTagDto.Title)),
            album: GetFieldValue(nameof(AudiobookTagDto.Album)),
            trackNumber: GetTrackNumberValue(),
            year: GetYearValue(),
            genre: GetFieldValue(nameof(AudiobookTagDto.Genre)),
            narrator: GetFieldValue(nameof(AudiobookTagDto.Narrator)),
            producer: GetFieldValue(nameof(AudiobookTagDto.Producer)),
            copyright: GetFieldValue(nameof(AudiobookTagDto.Copyright)),
            publisher: GetFieldValue(nameof(AudiobookTagDto.Publisher)),
            comment: GetFieldValue(nameof(AudiobookTagDto.Comment)),
            asin: GetFieldValue(nameof(AudiobookTagDto.ASIN)),
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
        foreach (var field in TagFields)
        {
            field.Value = string.Empty;
        }
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
}
