using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using Phantasma.Models;

namespace Phantasma.Binders;

/// <summary>
/// Screen Rendering System
/// Handles drawing sprites and managing the display.
/// </summary>
public class Screen
{
    private int screenWidth;
    private int screenHeight;
    private int tileWidth;
    private int tileHeight;

    // Rendering Mode
    public enum RenderMode
    {
        ColoredSquares,  // Fallback mode
        Sprites          // Sprite-based rendering
    }

    public RenderMode CurrentRenderMode { get; private set; }

    public Screen()
    {
        tileWidth = Dimensions.TILE_W;
        tileHeight = Dimensions.TILE_H;
        
        // Check if sprites are available.
        CurrentRenderMode = SpriteManager.HasSprites() ? 
            RenderMode.Sprites : RenderMode.ColoredSquares;
    }

    /// <summary>
    /// Initialize the screen and load sprites.
    /// </summary>
    public void Initialize()
    {
        // Load terrain sprites.
        SpriteManager.LoadTerrainSprites();
            
        // Load character sprites.
        SpriteManager.LoadCharacterSprites();
        
        // Update render mode based on sprite availability.
        CurrentRenderMode = SpriteManager.HasSprites() ? 
            RenderMode.Sprites : RenderMode.ColoredSquares;
            
        Console.WriteLine($"Screen initialized in {CurrentRenderMode} mode.");
    }

    /// <summary>
    /// Draw a terrain tile.
    /// </summary>
    public void DrawTerrain(DrawingContext context, int x, int y, Terrain terrain)
    {
        var destRect = new Rect(x * tileWidth, y * tileHeight, tileWidth, tileHeight);
        
        if (CurrentRenderMode == RenderMode.Sprites && terrain.Sprite?.SourceImage != null)
        {
            DrawSprite(context, terrain.Sprite, destRect);
        }
        else
        {
            DrawColoredTile(context, destRect, terrain);
        }
    }
        
    /// <summary>
    /// Draw a being (character, monster, etc.).
    /// </summary>
    public void DrawBeing(DrawingContext context, int x, int y, Being being)
    {
        var destRect = new Rect(x * tileWidth, y * tileHeight, tileWidth, tileHeight);
            
        if (CurrentRenderMode == RenderMode.Sprites && being.CurrentSprite.SourceImage != null)
        {
            DrawSprite(context, being.CurrentSprite, destRect);
        }
        else
        {
            DrawCharacterTile(context, destRect, being);
        }
    }

    /// <summary>
    /// Draw a sprite at the specified position.
    /// </summary>
    public void DrawSprite(DrawingContext context, Sprite sprite, Rect destRect)
    {
        if (sprite.SourceImage == null) return;
        
        // Source rectangle from the sprite sheet.
        var sourceRect = new Rect(
            sprite.SourceX, 
            sprite.SourceY, 
            sprite.WPix, 
            sprite.HPix);
        
        // Draw the sprite.
        context.DrawImage(
            sprite.SourceImage,
            sourceRect,
            destRect);
    }

    /// <summary>
    /// Fallback rendering with colored tiles.
    /// </summary>
    public void DrawColoredTile(DrawingContext context, Rect rect, Terrain terrain)
    {
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
            
        var textX = rect.X + (rect.Width - text.Width) / 2;
        var textY = rect.Y + (rect.Height - text.Height) / 2;
        
        context.DrawText(text, new Point(textX, textY));
    }
        
    /// <summary>
    /// Draw a character tile (ASCII fallback).
    /// </summary>
    private void DrawCharacterTile(DrawingContext context, Rect rect, Being being)
    {
        // Determine character and color based on being type.
        char displayChar = '@';  // Default for player
        string color = "#FFFF00"; // Yellow for player
            
        if (being is Character character)
        {
            if (character.IsPlayer)
            {
                displayChar = '@';
                color = "#FFFF00"; // Yellow
            }
            else
            {
                displayChar = 'h'; // 'h' for human NPC
                color = "#00FFFF"; // Cyan for NPCs
            }
        }
        else
        {
            displayChar = 'm'; // 'm' for monster
            color = "#FF0000"; // Red for enemies
        }
            
        // If sprite has a display char, use it.
        if (being.CurrentSprite.Tag != null)
        {
            displayChar = being.CurrentSprite.DisplayChar != '\0' ? 
                being.CurrentSprite.DisplayChar : displayChar;
        }
            
        // Draw the character.
        var text = new FormattedText(
            displayChar.ToString(),
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas", FontStyle.Normal, FontWeight.Bold),
            24,  // Slightly larger for characters
            new SolidColorBrush(Color.Parse(color)));
                
        var textX = rect.X + (rect.Width - text.Width) / 2;
        var textY = rect.Y + (rect.Height - text.Height) / 2;
            
        context.DrawText(text, new Point(textX, textY));
    }

    /// <summary>
    /// Draw the complete map view.
    /// </summary>
    public void DrawMap(DrawingContext context, Place place, int viewX = 0, int viewY = 0)
    {
        if (place == null) return;
        
        // Calculate visible area.
        int startX = Math.Max(0, viewX);
        int startY = Math.Max(0, viewY);
        int endX = Math.Min(place.Width, viewX + (screenWidth / tileWidth) + 1);
        int endY = Math.Min(place.Height, viewY + (screenHeight / tileHeight) + 1);
        
        // Layer 1: Draw terrain.
        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                var terrain = place.GetTerrain(x, y);
                if (terrain != null)
                {
                    DrawTerrain(context, x - viewX, y - viewY, terrain);
                }
            }
        }
        
        // Layer 2: Draw objects (items, containers, etc.).
        
        // Layer 3: Draw beings (player, NPCs, monsters).
        var beings = place.GetAllBeings();
        foreach (var being in beings)
        {
            if (being.GetX() >= startX && being.GetX() < endX &&
                being.GetY() >= startY && being.GetY() < endY)
            {
                DrawBeing(context, being.GetX() - viewX, being.GetY() - viewY, being);
            }
        }
    }

    /// <summary>
    /// Set screen dimensions.
    /// </summary>
    public void SetScreenSize(int width, int height)
    {
        screenWidth = width;
        screenHeight = height;
    }
}