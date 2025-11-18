using Avalonia.Controls;

namespace Phantasma.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        WindowState = WindowState.FullScreen;
        SystemDecorations = SystemDecorations.None;
    }
}