using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

using Phantasma.Models;

namespace Phantasma.Views;

public class GameView : Control
{
    private Session gameSession;  //Maybe Phantasma, instead?
    private const int TILE_SIZE = 32;  // Size of each tile in pixels

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
        // Set control size based on map.
        Width = 20 * TILE_SIZE;  // 20 tiles wide
        Height = 20 * TILE_SIZE; // 20 tiles high
        
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
            
        var place = gameSession.CurrentPlace;
        
        // Draw terrain grid.
        for (int y = 0; y < place.Height; y++)
        {
            for (int x = 0; x < place.Width; x++)
            {
                var terrain = place.GetTerrainAt(x, y);
                if (terrain != null)
                {
                    DrawTile(context, x, y, terrain);
                }
            }
        }
        
        // Draw grid lines (optional, helps visualize tiles).
        DrawGrid(context, place.Width, place.Height);
    }

    private void DrawTile(DrawingContext context, int x, int y, Terrain terrain)
    {
        // Calculate pixel position.
        var rect = new Rect(x * TILE_SIZE, y * TILE_SIZE, TILE_SIZE, TILE_SIZE);
        
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
            
        var textX = x * TILE_SIZE + (TILE_SIZE - text.Width) / 2;
        var textY = y * TILE_SIZE + (TILE_SIZE - text.Height) / 2;
        
        context.DrawText(text, new Point(textX, textY));
    }

    private void DrawGrid(DrawingContext context, int width, int height)
    {
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(32, 255, 255, 255)), 1);
        
        // Draw vertical lines.
        for (int x = 0; x <= width; x++)
        {
            context.DrawLine(pen, 
                new Point(x * TILE_SIZE, 0), 
                new Point(x * TILE_SIZE, height * TILE_SIZE));
        }
        
        // Draw horizontal lines.
        for (int y = 0; y <= height; y++)
        {
            context.DrawLine(pen, 
                new Point(0, y * TILE_SIZE), 
                new Point(width * TILE_SIZE, y * TILE_SIZE));
        }
    }
}
