using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Layout;
using Avalonia.Threading;

namespace Phantasma.Views;

/// <summary>
/// Splash Screen Window for Phantasma
/// Shows while the game engine is loading.
/// </summary>
public class SplashWindow : Window
{
    private Image splashImage;
    private TextBlock loadingText;
    private ProgressBar progressBar;

    public SplashWindow()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        // Window Properties
        Title = "Phantasma - Loading";
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        CanResize = false;
        WindowState = WindowState.Normal;
        SystemDecorations = SystemDecorations.None; // No title bar for splash
        Width = 640;
        Height = 480;
        
        // Create the layout.
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        
        // Splash Image
        splashImage = new Image
        {
            Stretch = Stretch.UniformToFill,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetRow(splashImage, 0);
        
        // Load text.
        loadingText = new TextBlock
        {
            Text = "Loading Phantasma...",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.White,
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 10)
        };
        Grid.SetRow(loadingText, 1);
        
        // Progress Bar
        progressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Height = 20,
            Margin = new Thickness(20, 10, 20, 20),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Grid.SetRow(progressBar, 2);
        
        // Add dark background.
        Background = new SolidColorBrush(Color.Parse("#1a1a1a"));
        
        // Add components to grid.
        grid.Children.Add(splashImage);
        grid.Children.Add(loadingText);
        grid.Children.Add(progressBar);
        
        Content = grid;
    }

    /// <summary>
    /// Load splash image from file.
    /// </summary>
    public async Task LoadSplashImage(string imagePath)
    {
        try
        {
            if (!string.IsNullOrEmpty(imagePath) && System.IO.File.Exists(imagePath))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var bitmap = new Bitmap(imagePath);
                    splashImage.Source = bitmap;
                });
            }
            else
            {
                // Use default/embedded splash image.
                await LoadDefaultSplash();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load splash image: {ex.Message}");
            await LoadDefaultSplash();
        }
    }

    /// <summary>
    /// Load a default splash screen if custom image fails.
    /// </summary>
    private async Task LoadDefaultSplash()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Create a simple gradient background as default.
            var gradient = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative)
            };
            gradient.GradientStops.Add(new GradientStop(Color.Parse("#2a2a3a"), 0));
            gradient.GradientStops.Add(new GradientStop(Color.Parse("#1a1a2a"), 1));
            
            Background = gradient;
            
            // Update text to be more visible.
            loadingText.Foreground = Brushes.LightGray;
            loadingText.FontSize = 24;
            loadingText.Text = "PHANTASMA";
        });
    }

    /// <summary>
    /// Update loading progress.
    /// </summary>
    public async Task UpdateProgress(double progress, string message = null)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            progressBar.Value = Math.Min(100, Math.Max(0, progress));
            
            if (!string.IsNullOrEmpty(message))
            {
                loadingText.Text = message;
            }
        });
    }

    /// <summary>
    /// Show splash screen and execute loading tasks.
    /// </summary>
    public async Task ShowSplashAsync(Func<IProgress<(double, string)>, Task> loadingTask)
    {
        Show();
        
        // Create progress reporter.
        var progress = new Progress<(double percent, string message)>(report =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                progressBar.Value = report.percent;
                if (!string.IsNullOrEmpty(report.message))
                    loadingText.Text = report.message;
            });
        });
        
        try
        {
            // Execute the loading task.
            await loadingTask(progress);
        }
        finally
        {
            // Close splash screen.
            Close();
        }
    }
}
