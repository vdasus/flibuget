using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Reflection;

namespace flibuget.Views;

public partial class MainWindow : Window
{
    private const string APP_NAME = "flibuget";

    public MainWindow()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version;
#if DEBUG
        Title = $"{APP_NAME} v{version}d";
#else
        Title = $"{APP_NAME} v{version}";
#endif
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
