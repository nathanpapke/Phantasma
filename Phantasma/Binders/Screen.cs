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
    private Cursor cursor;      // For blinking text cursor sprite
    private Crosshair crosshair;    // For targeting system
    
    public Cursor Cursor => cursor;
    public Crosshair Crosshair => crosshair;

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
        
        // Initialize UI elements.
        cursor = new Cursor();
        crosshair = new Crosshair();
        
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
    
        // Get sprite from the item's type.
        var sprite = item.Type?.Sprite;
    
        if (CurrentRenderMode == RenderMode.Sprites && sprite?.SourceImage != null)
        {
            DrawSprite(context, sprite, destRect);
        }
        else
        {
            // Fallback: draw a simple marker.
            DrawItemFallback(context, destRect, item);
        }
    }

    /// <summary>
    /// Fallback rendering for items without sprites.
    /// </summary>
    private void DrawItemFallback(DrawingContext context, Rect rect, Item item)
    {
        // Draw a small colored square in the center of the tile.
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
    /// Draw a missile (arrow, bolt, etc. in flight).
    /// </summary>
    public void DrawMissile(DrawingContext context, int viewX, int viewY, Missile missile)
    {
        var destRect = new Rect(viewX * tileWidth, viewY * tileHeight, tileWidth, tileHeight);
    
        var sprite = missile.Sprite;
    
        if (CurrentRenderMode == RenderMode.Sprites && sprite?.SourceImage != null)
        {
            DrawSprite(context, sprite, destRect);
        }
        else
        {
            // Fallback: draw a simple projectile indicator.
            DrawMissileDot(context, destRect, missile);
        }
    }

    /// <summary>
    /// Fallback rendering for missiles without sprites.
    /// </summary>
    private void DrawMissileDot(DrawingContext context, Rect rect, Missile missile)
    {
        // Draw a small white dot in center of tile.
        var centerX = rect.X + rect.Width / 2;
        var centerY = rect.Y + rect.Height / 2;
        var radius = Math.Min(rect.Width, rect.Height) / 4;

        context.DrawEllipse(Brushes.White, null, new Point(centerX, centerY), radius, radius);
    }
    
    /// <summary>
    /// Draw the targeting crosshair).
    /// </summary>
    public void DrawCrosshair(DrawingContext context, int x, int y, Cursor cursor)
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
            DrawHardcodedCrosshair(context, destRect);
        }
    }

    private void DrawHardcodedCrosshair(DrawingContext context, Rect destRect)
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
    /// Animate a projectile flying from origin to target.
    /// Uses Bresenham's line algorithm to move pixel-by-pixel.
    /// Calls missile.EnterTile() for collision detection at each tile boundary.
    /// </summary>
    /// <param name="originX">Starting map X coordinate</param>
    /// <param name="originY">Starting map Y coordinate</param>
    /// <param name="targetX">Target map X coordinate</param>
    /// <param name="targetY">Target map Y coordinate</param>
    /// <param name="sprite">Projectile sprite to render</param>
    /// <param name="place">The map/place</param>
    /// <param name="missile">Missile object for collision detection</param>
    /// <returns>Final position where missile stopped (may be before target if hit)</returns>
    public (int X, int Y) AnimateProjectile(
        int originX, int originY, 
        int targetX, int targetY,
        Sprite? sprite, 
        Place place, 
        Missile missile)
    {
        // Track current position in map coordinates as we fly.
        int currentMapX = originX;
        int currentMapY = originY;
        int prevMapX = originX;
        int prevMapY = originY;
        
        // Get viewport offset (where map origin is on screen).
        int viewStartX = (screenWidth / tileWidth) / 2;
        int viewStartY = (screenHeight / tileHeight) / 2;
        
        // Convert map coordinates to screen pixel coordinates.
        int screenOriginX = (originX - viewStartX) * tileWidth;
        int screenOriginY = (originY - viewStartY) * tileHeight;
        int screenTargetX = (targetX - viewStartX) * tileWidth;
        int screenTargetY = (targetY - viewStartY) * tileHeight;
        
        // Current screen pixel position (starts at origin).
        int screenX = screenOriginX;
        int screenY = screenOriginY;
        
        // Calculate deltas.
        int deltaX = screenTargetX - screenOriginX;
        int deltaY = screenTargetY - screenOriginY;
        int absDeltaX = Math.Abs(deltaX);
        int absDeltaY = Math.Abs(deltaY);
        
        // Set sprite orientation based on direction of travel.
        if (sprite != null)
        {
            int facing = DirectionFromVector(deltaX, deltaY);
            sprite.Facing = facing;
        }
        
        // Determine step direction (+1 or -1).
        int stepX = (screenOriginX > screenTargetX) ? -1 : 1;
        int stepY = (screenOriginY > screenTargetY) ? -1 : 1;
        
        // Bresenham's Line Algorithm
        // Walk along the dominant axis (X or Y).
        if (absDeltaX >= absDeltaY)
        {
            // Walk along X axis.
            int deltaP = absDeltaY << 1;
            int deltaPIncr = deltaP - (absDeltaX << 1);
            int p = deltaP - absDeltaX;
            
            for (int i = absDeltaX; i >= 0; i--)
            {
                // Check if we've entered a new tile.
                prevMapX = currentMapX;
                prevMapY = currentMapY;
                currentMapX = (screenX - screenOriginX + originX * tileWidth) / tileWidth;
                currentMapY = (screenY - screenOriginY + originY * tileHeight) / tileHeight;
                
                // If changed tiles, do collision detection.
                if (currentMapX != prevMapX || currentMapY != prevMapY)
                {
                    if (!missile.EnterTile(place, currentMapX, currentMapY))
                    {
                        // Hit something - stop here
                        return (currentMapX, currentMapY);
                    }
                }
                
                // Paint missile sprite at current position (if visible).
                if (IsTileVisibleInViewport(currentMapX, currentMapY, place))
                {
                    PaintProjectile(screenX, screenY, sprite);
                }
                
                // Move to next pixel.
                if (p > 0)
                {
                    screenX += stepX;
                    screenY += stepY;
                    p += deltaPIncr;
                }
                else
                {
                    screenX += stepX;
                    p += deltaP;
                }
            }
        }
        else
        {
            // Walk along Y axis.
            int deltaP = absDeltaX << 1;
            int deltaPIncr = deltaP - (absDeltaY << 1);
            int p = deltaP - absDeltaY;
            
            for (int i = absDeltaY; i >= 0; i--)
            {
                // Check if we've entered a new tile.
                prevMapX = currentMapX;
                prevMapY = currentMapY;
                currentMapX = (screenX - screenOriginX + originX * tileWidth) / tileWidth;
                currentMapY = (screenY - screenOriginY + originY * tileHeight) / tileHeight;
                
                // If changed tiles, do collision detection.
                if (currentMapX != prevMapX || currentMapY != prevMapY)
                {
                    if (!missile.EnterTile(place, currentMapX, currentMapY))
                    {
                        // Hit something - stop here.
                        return (currentMapX, currentMapY);
                    }
                }
                
                // Paint missile sprite at current position (if visible).
                if (IsTileVisibleInViewport(currentMapX, currentMapY, place))
                {
                    PaintProjectile(screenX, screenY, sprite);
                }
                
                // Move to next pixel.
                if (p > 0)
                {
                    screenX += stepX;
                    screenY += stepY;
                    p += deltaPIncr;
                }
                else
                {
                    screenY += stepY;
                    p += deltaP;
                }
            }
        }
        
        // Restore sprite to default facing.
        if (sprite != null)
        {
            sprite.Facing = 0; // SPRITE_DEF_FACING
        }
        
        // Return final position (reached target).
        return (currentMapX, currentMapY);
    }
    
    /// <summary>
    /// Paint a projectile sprite at screen coordinates.
    /// Handles rendering with brief pause so player can see it.
    /// </summary>
    private void PaintProjectile(int screenX, int screenY, Sprite? sprite)
    {
        if (sprite == null)
            return;
        
        // Check if on screen.
        if (screenX < 0 || screenY < 0 || 
            screenX + tileWidth > screenWidth || 
            screenY + tileHeight > screenHeight)
            return;
        
        // TODO: Actual rendering implementation
        // This would need access to the DrawingContext
        // For now, this is a placeholder
        
        // In full implementation:
        // 1. Save background at this position
        // 2. Draw sprite
        // 3. Update screen
        // 4. Brief pause (1-2ms) so player can see it
        // 5. Restore background
        // 6. Update screen again
        
        System.Threading.Thread.Sleep(1); // Brief pause
    }
    
    /// <summary>
    /// Check if a map tile is visible in the current viewport.
    /// </summary>
    private bool IsTileVisibleInViewport(int mapX, int mapY, Place place)
    {
        // Check map bounds.
        if (mapX < 0 || mapX >= place.Width || mapY < 0 || mapY >= place.Height)
            return false;
        
        // Check if in viewport.
        int viewStartX = (screenWidth / tileWidth) / 2;
        int viewStartY = (screenHeight / tileHeight) / 2;
        int viewEndX = viewStartX + (screenWidth / tileWidth);
        int viewEndY = viewStartY + (screenHeight / tileHeight);
        
        return mapX >= viewStartX && mapX < viewEndX && 
               mapY >= viewStartY && mapY < viewEndY;
    }
    
    /// <summary>
    /// Convert a direction vector to a facing index.
    /// </summary>
    private int DirectionFromVector(int dx, int dy)
    {
        // Nazghul uses 8 directions (N, NE, E, SE, S, SW, W, NW).
        // Map dx/dy to direction index.
        
        if (dx == 0 && dy < 0) return 0;  // North
        if (dx > 0 && dy < 0) return 1;   // Northeast
        if (dx > 0 && dy == 0) return 2;  // East
        if (dx > 0 && dy > 0) return 3;   // Southeast
        if (dx == 0 && dy > 0) return 4;  // South
        if (dx < 0 && dy > 0) return 5;   // Southwest
        if (dx < 0 && dy == 0) return 6;  // West
        if (dx < 0 && dy < 0) return 7;   // Northwest
        
        return 0; // Default to North
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
        
        // Layer 4: Draw missiles (arrows in flight)
        var missiles = place.GetAllMissiles();
        foreach (var missile in missiles)
        {
            viewX = missile.GetX() - viewStartX;
            viewY = missile.GetY() - viewStartY;

            if (viewX >= 0 && viewX < tilesWide &&
                viewY >= 0 && viewY < tilesHigh)
            {
                if (IsTileVisible(vmask, viewX, viewY))
                {
                    DrawMissile(context, viewX, viewY, missile);
                }
            }
        }
        
        // Layer 5: Draw cursor if active.
        // Add this after the beings loop ends and before the closing brace of DrawMap()
        if (crosshair != null && crosshair.IsActive())
        {
            viewX = crosshair.GetX() - viewStartX;
            viewY = crosshair.GetY() - viewStartY;
            
            if (viewX >= 0 && viewX < tilesWide &&
                viewY >= 0 && viewY < tilesHigh)
            {
                DrawCrosshair(context, viewX, viewY, cursor);
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