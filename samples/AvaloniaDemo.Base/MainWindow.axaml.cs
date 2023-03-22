using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaloniaDemo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }
}
