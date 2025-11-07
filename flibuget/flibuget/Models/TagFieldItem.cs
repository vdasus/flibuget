using CommunityToolkit.Mvvm.ComponentModel;

namespace flibuget.Models;

/// <summary>
/// Represents a single tag field for dynamic UI binding.
/// </summary>
public partial class TagFieldItem : ObservableObject
{
    private string _propertyName = string.Empty;
    private string _displayName = string.Empty;
    private string _value = string.Empty;
    private string _watermark = string.Empty;
    private bool _isMultiLine;

    /// <summary>
    /// Property name in the DTO (e.g., "Author", "Title").
    /// </summary>
    public string PropertyName
    {
        get => _propertyName;
        set => SetProperty(ref _propertyName, value);
    }

    /// <summary>
    /// Display name to show in the UI (e.g., "Author", "ASIN").
    /// </summary>
    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    /// <summary>
    /// Current value of the tag.
    /// </summary>
    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    /// <summary>
    /// Watermark/placeholder text for the input.
    /// </summary>
    public string Watermark
    {
        get => _watermark;
        set => SetProperty(ref _watermark, value);
    }

    /// <summary>
    /// Whether this field should be rendered as a multi-line text box.
    /// </summary>
    public bool IsMultiLine
    {
        get => _isMultiLine;
        set => SetProperty(ref _isMultiLine, value);
    }

    /// <summary>
    /// Display order (for sorting).
    /// </summary>
    public int Order { get; set; }
}
