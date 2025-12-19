using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

using Phantasma.Binders;

namespace Phantasma.Views;

/// <summary>
/// View for the game map area.
/// IMPORTANT: This View only knows about Screen (the Binder).
/// It NEVER references any classes from Phantasma.Models.
/// </summary>
public class GameView : Control
{
    private Screen screen;

    public GameView()
    {
        // Initialize screen renderer (our Binder).
        screen = new Screen();
        screen.Initialize();
        
        // Set control size based on map.
        Width = 20 * screen.TileWidth;
        Height = 20 * screen.TileHeight;
            
        screen.SetScreenSize((int)Width, (int)Height);
        
        // Set up render timer.
        var timer = new DispatcherTimer();
        timer.Interval = TimeSpan.FromMilliseconds(100);  // 10 FPS
        timer.Tick += (s, e) => InvalidateVisual();
        timer.Start();
    }

    /// <summary>
    /// Get the Screen binder so it can be bound to a Session externally.
    /// </summary>
    public Screen GetScreen() => screen;

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        // Clear background.
        context.FillRectangle(Brushes.Black, new Rect(0, 0, Bounds.Width, Bounds.Height));
        
        // Let the Screen binder handle all rendering.
        screen.Render(context, Bounds);
        
        // Draw grid lines if in fallback mode.
        if (screen.CurrentRenderMode == Screen.RenderMode.ColoredSquares)
        {
            DrawGrid(context);
        }
    }

    private void DrawGrid(DrawingContext context)
    {
        int tilesWide = screen.TilesWide;
        int tilesHigh = screen.TilesHigh;
        int tileWidth = screen.TileWidth;
        int tileHeight = screen.TileHeight;
        
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(32, 255, 255, 255)), 1);
        
        // Draw vertical lines.
        for (int x = 0; x <= tilesWide; x++)
        {
            context.DrawLine(pen, 
                new Point(x * tileWidth, 0), 
                new Point(x * tileWidth, tilesHigh * tileHeight));
        }
        
        // Draw horizontal lines.
        for (int y = 0; y <= tilesHigh; y++)
        {
            context.DrawLine(pen, 
                new Point(0, y * tileHeight), 
                new Point(tilesWide * tileWidth, y * tileHeight));
        }
    }
}
