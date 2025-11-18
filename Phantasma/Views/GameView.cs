using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

using Phantasma.Models;
using Phantasma.Binders;

namespace Phantasma.Views;

public class GameView : Control
{
    private Session gameSession;  //Maybe Phantasma, instead?
    private Screen screen;

    public Session GameSession
    {
        get => gameSession;
        set
        {
            gameSession = value;
            InvalidateVisual();  // Redraw when session changes
        }
    }

    public GameView()
    {
        // Initialize screen renderer.
        screen = new Screen();
        screen.Initialize();
        
        // Set control size based on map.
        Width = 20 * Dimensions.TILE_W;  // 20 tiles wide
        Height = 20 * Dimensions.TILE_H; // 20 tiles high
            
        screen.SetScreenSize((int)Width, (int)Height);
        
        // Set up render timer.
        var timer = new DispatcherTimer();
        timer.Interval = TimeSpan.FromMilliseconds(100);  // 10 FPS
        timer.Tick += (s, e) => InvalidateVisual();
        timer.Start();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        // Clear background.
        context.FillRectangle(Brushes.Black, new Rect(0, 0, Width, Height));
        
        if (gameSession?.CurrentPlace == null)
            return;
            
        // Use the Screen class to render.
        screen.DrawMap(context, gameSession.CurrentPlace);
            
        // Draw grid lines (optional, helps visualize tiles).
        if (screen.CurrentRenderMode == Screen.RenderMode.ColoredSquares)
        {
            DrawGrid(context, gameSession.CurrentPlace.Width, gameSession.CurrentPlace.Height);
        }
    }

    private void DrawTile(DrawingContext context, int x, int y, Terrain terrain)
    {
        // Calculate pixel position.
        var rect = new Rect(x * Dimensions.TILE_W, y * Dimensions.TILE_H,
            Dimensions.TILE_W, Dimensions.TILE_H);
        
        // Parse color and create brush.
        var color = Color.Parse(terrain.Color);
        var brush = new SolidColorBrush(color);
        
        // Fill tile with terrain color.
        context.FillRectangle(brush, rect);
        
        // Draw character in center of tile.
        var text = new FormattedText(
            terrain.DisplayChar.ToString(),
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas", FontStyle.Normal, FontWeight.Bold),
            20,
            Brushes.White);
            
        var textX = x * Dimensions.TILE_W + (Dimensions.TILE_W - text.Width) / 2;
        var textY = y * Dimensions.TILE_H + (Dimensions.TILE_H - text.Height) / 2;
        
        context.DrawText(text, new Point(textX, textY));
    }

    private void DrawGrid(DrawingContext context, int width, int height)
    {
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(32, 255, 255, 255)), 1);
        
        // Draw vertical lines.
        for (int x = 0; x <= width; x++)
        {
            context.DrawLine(pen, 
                new Point(x * Dimensions.TILE_W, 0), 
                new Point(x * Dimensions.TILE_W, height * Dimensions.TILE_H));
        }
        
        // Draw horizontal lines.
        for (int y = 0; y <= height; y++)
        {
            context.DrawLine(pen, 
                new Point(0, y * Dimensions.TILE_H), 
                new Point(width * Dimensions.TILE_W, y * Dimensions.TILE_H));
        }
    }
}
