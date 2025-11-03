using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;

namespace flibuget.Models;

/// <summary>
/// Represents a single audio file in the audiobook project
/// </summary>
public partial class AudiobookFile : ObservableObject
{
    [ObservableProperty]
    private string filePath = string.Empty;

    [ObservableProperty]
    private string fileName = string.Empty;

    [ObservableProperty]
    private bool isSelected;

    public AudiobookFile(string filePath)
    {
        this.filePath = filePath;
        fileName = Path.GetFileName(filePath);
    }
}
