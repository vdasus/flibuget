using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using flibuget.Core.Examples;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using flibuget.Core.InfraServices.AudioTags; // added

namespace flibuget.ViewModels;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public partial class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel>? _logger;
    private readonly IServiceProvider? _serviceProvider;
    private readonly IAudioTagService? _tagService; // added
    private static readonly HttpClient _httpClient = new();

    [ObservableProperty]
    private string greeting = "Flibuget!";

    [ObservableProperty]
    private string author = string.Empty;

    [ObservableProperty]
    private string book = string.Empty;

    [ObservableProperty]
    private string result = string.Empty;

    [ObservableProperty]
    private string imageUrl = string.Empty;

    [ObservableProperty]
    private Bitmap? coverImage;

    public object ClickCommand { get; }
    public object TestCommand { get; }

    // Parameterless constructor for designer support
    public MainViewModel() : this(null!, null!, null!) { }

    // Backward compatibility constructor (tests still use this)
    public MainViewModel(ILogger<MainViewModel> logger, IServiceProvider serviceProvider)
        : this(logger, serviceProvider, null!) { }

    public MainViewModel(ILogger<MainViewModel> logger, IServiceProvider serviceProvider, IAudioTagService tagService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _tagService = tagService; // may be null in tests / design

        _logger?.LogInformation("MainViewModel initialized");
        _logger?.LogDebug("Greeting message: {Greeting}", Greeting);

        _logger?.LogInformation("Application started for user {UserName} on machine {MachineName}",
              Environment.UserName,
              Environment.MachineName);

        ClickCommand = new AsyncRelayCommand(OnButtonClickAsync);
        TestCommand = new AsyncRelayCommand(OnTestButtonClickAsync);
    }

    private Task OnTestButtonClickAsync()
    {
        var file = "D:\\_TEMP\\mp3tag\\temp\\01_Opergruppa_v_beriozovke.mp3";
        var tmp = _tagService.Read(file.Trim());
        return default;
    }

    private async Task OnButtonClickAsync()
    {
        Result = $"Searching for: {Book} by {Author}...";
        ImageUrl = string.Empty;
        CoverImage = null;

        try
        {
            if (_serviceProvider == null)
            {
                Result = "Service provider not available (design mode)";
                return;
            }

            var dto = await AIServiceUsageExamplesForAudiobooks.Example3_AudioBookStructuredJsonResponseAsync(_serviceProvider, Book, Author).ConfigureAwait(true);
            Result =
                $"Title: {dto.Title}\nAuthor: {dto.Author}\nDuration: {dto.Duration}\nNarrator: {dto.Narrator}\nAge Restriction: {dto.AgeRestriction}\nDescription: {dto.Description}\nThemes: {string.Join(", ", dto.Themes ?? new List<string>())}\nLink: {dto.Link}\nImgUrl: {dto.CoverLink}";

            ImageUrl = dto.CoverLink ?? string.Empty;

            // Load image from URL
            if (!string.IsNullOrWhiteSpace(dto.CoverLink))
            {
                try
                {
                    var imageBytes = await _httpClient.GetByteArrayAsync(dto.CoverLink).ConfigureAwait(false);
                    using var ms = new MemoryStream(imageBytes);
                    CoverImage = new Bitmap(ms);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to load cover image from {Url}", dto.CoverLink);
                    CoverImage = null;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching audiobook data");
            Result = $"Error fetching data: {ex.Message}";
            ImageUrl = string.Empty;
            CoverImage = null;
        }
    }
}
