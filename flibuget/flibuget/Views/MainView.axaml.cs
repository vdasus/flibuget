using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using flibuget.ViewModels;
using System.Threading.Tasks;

namespace flibuget.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        DataContextChanged += MainView_DataContextChanged;
    }

    private void MainView_DataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            // Wire up file picker functions
            viewModel.FolderPickerFunc = PickFolderAsync;
            viewModel.SaveFilePickerFunc = PickSaveFileAsync;
            viewModel.ImageFilePickerFunc = PickImageFileAsync;
        }
    }

    private async Task<string?> PickFolderAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Audiobook Folder",
            AllowMultiple = false
        }).ConfigureAwait(false);

        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }

    private async Task<string?> PickSaveFileAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Project",
            DefaultExtension = "json",
            SuggestedFileName = "audiobook-project.json",
            FileTypeChoices =
            [
                new FilePickerFileType("JSON Project File")
                {
                    Patterns = ["*.json"]
                }
            ]
        }).ConfigureAwait(false);

        return file?.Path.LocalPath;
    }

    private async Task<string?> PickImageFileAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Cover Image",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Image Files")
                {
                    Patterns = ["*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif"]
                }
            ]
        }).ConfigureAwait(false);

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    private async void OnCoverImageTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            await viewModel.SelectCoverImageCommand.ExecuteAsync(null).ConfigureAwait(false);
        }
    }

    // Added missing InitializeComponent so NCrunch/build can find it
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
