namespace flibuget.Core.Domain.Attributes;

/// <summary>
/// Attribute to control how a tag property is displayed in the UI.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class TagDisplayAttribute : Attribute
{
    /// <summary>
    /// Display order in the UI. Lower numbers appear first.
    /// If not specified, properties appear in alphabetical order after ordered ones.
    /// </summary>
    public int Order { get; set; } = int.MaxValue;

    /// <summary>
    /// Display name shown in the UI. If not specified, the property name is used with spacing.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Whether this field should be rendered as a multi-line text box.
    /// </summary>
    public bool IsMultiLine { get; set; }

    /// <summary>
    /// Whether this property should be ignored and not displayed in the UI.
    /// </summary>
    public bool Ignore { get; set; }

    /// <summary>
    /// Watermark/placeholder text for the input field.
    /// </summary>
    public string? Watermark { get; set; }
}
