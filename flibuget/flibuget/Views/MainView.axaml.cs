using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace flibuget.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    // Added missing InitializeComponent so NCrunch/build can find it
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
