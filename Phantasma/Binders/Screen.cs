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
    private VisibilityMask visibilityCache = new VisibilityMask();
    
    // UI State
    private Cursor cursor;
    
    public Cursor Cursor => cursor;

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
        
        // Initialize cursor.
        cursor = new Cursor();
        
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
    /// Check if a viewport tile is visible.
    /// </summary>
    private bool IsTileVisible(byte[] vmask, int posX, int posY)
    {
        // The vmask is 39x39, viewport varies by screen size.
        int vmaskX = posX + (39 - screenWidth / tileWidth) / 2;
        int vmaskY = posY +  (39 - screenHeight / tileHeight) / 2;
    
        if (vmaskX < 0 || vmaskX >= 39 || vmaskY < 0 || vmaskY >= 39)
            return false;
    
        int index = vmaskY * 39 + vmaskX;
        return index < vmask.Length && vmask[index] > 0; // removed index >= 0 &&
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
    /// Draw black fog over invisible tiles.
    /// </summary>
    private void DrawFog(DrawingContext context, int x, int y)
    {
        var rect = new Rect(
            x * tileWidth,
            y * tileHeight,
            tileWidth,
            tileHeight
        );
        context.FillRectangle(Brushes.Black, rect);
    }
    
    /// <summary>
    /// Draw an item (gold, potions, weapons, etc.).
    /// </summary>
    public void DrawItem(DrawingContext context, int x, int y, Item item)
    {
        var destRect = new Rect(x * tileWidth, y * tileHeight, tileWidth, tileHeight);
    
        // Get sprite from the item's type
        var sprite = item.Type?.Sprite;
    
        if (CurrentRenderMode == RenderMode.Sprites && sprite?.SourceImage != null)
        {
            DrawSprite(context, sprite, destRect);
        }
        else
        {
            // Fallback: draw a simple marker
            DrawItemFallback(context, destRect, item);
        }
    }

    /// <summary>
    /// Fallback rendering for items without sprites.
    /// </summary>
    private void DrawItemFallback(DrawingContext context, Rect rect, Item item)
    {
        // Draw a small colored square in the center of the tile
        var itemRect = new Rect(
            rect.X + rect.Width / 4,
            rect.Y + rect.Height / 4,
            rect.Width / 2,
            rect.Height / 2
        );
    
        context.FillRectangle(Brushes.Yellow, itemRect);
    }
        
    /// <summary>
    /// Draw a being (character, monster, etc.).
    /// </summary>
    public void DrawBeing(DrawingContext context, int x, int y, Being being)
    {
        var destRect = new Rect(x * tileWidth, y * tileHeight, tileWidth, tileHeight);
            
        if (CurrentRenderMode == RenderMode.Sprites && being.CurrentSprite?.SourceImage != null)
        {
            DrawSprite(context, being.CurrentSprite, destRect);
        }
        else
        {
            DrawCharacterTile(context, destRect, being);
        }
    }
    
    /// <summary>
    /// Draw the cursor (targeting crosshair).
    /// </summary>
    public void DrawCursor(DrawingContext context, int x, int y, Cursor cursor)
    {
        var destRect = new Rect(x * tileWidth, y * tileHeight, tileWidth, tileHeight);
    
        // Look up crosshair type from registry.
        var crosshairType = Phantasma.GetRegisteredObject("crosshair") as ObjectType;
    
        // Use configured crosshair sprite if available.
        if (CurrentRenderMode == RenderMode.Sprites && crosshairType?.Sprite?.SourceImage != null)
        {
            DrawSprite(context, crosshairType.Sprite, destRect);
        }
        else
        {
            // Fallback: draw hardcoded crosshair graphic.
            DrawCrosshair(context, destRect);
        }
    }

    private void DrawCrosshair(DrawingContext context, Rect destRect)
    {
        var pen = new Pen(Brushes.Red, 2);
    
        // Vertical Line
        context.DrawLine(pen, 
            new Point(destRect.X + destRect.Width / 2, destRect.Y),
            new Point(destRect.X + destRect.Width / 2, destRect.Y + destRect.Height));
    
        // Horizontal Line
        context.DrawLine(pen,
            new Point(destRect.X, destRect.Y + destRect.Height / 2),
            new Point(destRect.X + destRect.Width, destRect.Y + destRect.Height / 2));
    
        // Corner brackets for better visibility...
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
    public void DrawMap(DrawingContext context, Place place, int centerX, int centerY)
    {
        if (place == null) return;
        
        // Get visibility mask.
        byte[] vmask = visibilityCache.Get(place, centerX, centerY);
    
        // Calculate how many tiles fit in the viewport
        int tilesWide = screenWidth / tileWidth;
        int tilesHigh = screenHeight / tileHeight;
    
        // Calculate the top-left corner of our view in map coordinates
        int viewStartX = centerX - tilesWide / 2;
        int viewStartY = centerY - tilesHigh / 2;
        
        int viewX = 0;
        int viewY = 0;
        
        // Layer 1: Draw terrain.
        for (viewY = 0; viewY < tilesHigh; viewY++)
        {
            for (viewX = 0; viewX < tilesWide; viewX++)
            {
                // Calculate map coordinates
                int mapX = viewStartX + viewX;
                int mapY = viewStartY + viewY;
                
                var terrain = place.GetTerrain(mapX, mapY);
            
                // Calculate screen position (where to draw on screen)
                int screenX = viewX * tileWidth;
                int screenY = viewY * tileHeight;
                
                // Check if this map position is valid
                if (mapX < 0 || mapX >= place.Width || mapY < 0 || mapY >= place.Height)
                {
                    // Draw fog for out of bounds.
                    DrawFog(context, screenX, screenY);
                    continue;
                }
                
                if (terrain != null && IsTileVisible(vmask, viewX, viewY))
                {
                    DrawTerrain(context, viewX, viewY, terrain);
                }
                else
                {
                    DrawFog(context, viewX, viewY);
                }
            }
        }
        
        // Layer 2: Draw objects (items, containers, etc.).
        var items = place.GetAllItems();
        foreach (var item in items)
        {
            viewX = item.GetX() - viewStartX;
            viewY = item.GetY() - viewStartY;
    
            if (viewX >= 0 && viewX < (screenWidth / tileWidth) &&
                viewY >= 0 && viewY < (screenHeight / tileHeight))
            {
                if (IsTileVisible(vmask, viewX, viewY))
                {
                    DrawItem(context, viewX, viewY, item);
                }
            }
        }
        
        // Layer 3: Draw beings (player, NPCs, monsters).
        var beings = place.GetAllBeings();
        foreach (var being in beings)
        {
            viewX = being.GetX() - viewStartX;
            viewY = being.GetY() - viewStartY;
        
            if (viewX >= 0 && viewX < (screenWidth / tileWidth) &&
                viewY >= 0 && viewY < (screenHeight / tileHeight))
            {
                if (IsTileVisible(vmask, viewX, viewY))
                {
                    DrawBeing(context, viewX, viewY, being);
                }
            }
        }
        
        // Layer 4: Draw cursor if active.
        // Add this after the beings loop ends and before the closing brace of DrawMap()
        if (cursor != null && cursor.IsActive())
        {
            viewX = cursor.GetX() - viewStartX;
            viewY = cursor.GetY() - viewStartY;
            
            if (viewX >= 0 && viewX < tilesWide &&
                viewY >= 0 && viewY < tilesHigh)
            {
                DrawCursor(context, viewX, viewY, cursor);
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