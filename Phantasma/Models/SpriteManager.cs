using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace Phantasma.Models;

/// <summary>
/// Sprite Manager to Load and Cache Sprites
/// </summary>
public class SpriteManager
{
    private static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private static Dictionary<string, Bitmap> imageCache = new Dictionary<string, Bitmap>();

    /// <summary>
    /// Load a sprite sheet image.
    /// </summary>
    public static Bitmap? LoadImage(string filename)
    {
        if (imageCache.ContainsKey(filename))
            return imageCache[filename];
        
        string path = Phantasma.ResolvePath(filename);
        
        if (!File.Exists(path))
        {
            Console.WriteLine($"[SpriteManager] Not found: {filename}");
            Console.WriteLine($"[SpriteManager]   Expected: {path}");
            return null;
        }
        
        using var skBitmap = SKBitmap.Decode(path);
        
        for (int y = 0; y < skBitmap.Height; y++)
        {
            for (int x = 0; x < skBitmap.Width; x++)
            {
                var pixel = skBitmap.GetPixel(x, y);
                if (pixel.Red == 255 && pixel.Green == 0 && pixel.Blue == 255)
                {
                    skBitmap.SetPixel(x, y, SKColors.Transparent);
                }
            }
        }
        
        using var data = skBitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream(data.ToArray());
        
        var bitmap = new Bitmap(stream);
        imageCache[filename] = bitmap;
        Console.WriteLine($"[SpriteManager] Loaded: {filename}");
        return bitmap;
    }

    /// <summary>
    /// Create terrain sprites from a sprite sheet.
    /// Assumes 32x32 tiles in a grid.
    /// </summary>
    public static void LoadTerrainSprites()
    {
        // Load a sprite sheet (you'll need to provide terrain.png).
        var terrainSheet = LoadImage("terrain.png");
        
        if (terrainSheet != null)
        {
            // Define sprite positions in the sheet (32x32 tiles).
            // These positions would match your actual sprite sheet layout.
            RegisterSprite("grass", terrainSheet, 0, 0);      // First tile
            RegisterSprite("tree", terrainSheet, 32, 0);      // Second tile
            RegisterSprite("water", terrainSheet, 64, 0);     // Third tile
            RegisterSprite("mountain", terrainSheet, 96, 0);  // Fourth tile
            RegisterSprite("stone", terrainSheet, 128, 0);    // Fifth tile
            RegisterSprite("dirt", terrainSheet, 160, 0);     // Sixth tile
        }
        else
        {
            Console.WriteLine("Using colored squares as fallback (no sprite sheet found).");
            // We'll fall back to colored squares if no sprites available.
        }
    }

    public static void LoadCharacterSprites()
    {
        // Try to load character sprites.
        var spriteSheet = SpriteManager.LoadImage("characters.png");
        if (spriteSheet != null)
        {
            // Register player sprite (assuming it's at position 0,0 in the sheet).
            SpriteManager.RegisterSprite("player", spriteSheet, 0, 0);
                
            // Register NPC sprites.
            SpriteManager.RegisterSprite("npc", spriteSheet, 32, 0);
            SpriteManager.RegisterSprite("enemy", spriteSheet, 64, 0);
        }
        else
        {
            Console.WriteLine("No character sprites found, will use ASCII characters.");
        }
    }
    
    /// <summary>
    /// Register a sprite from a sprite sheet.
    /// </summary>
    public static void RegisterSprite(string tag, Bitmap sheet, int x, int y, int width = 32, int height = 32)
    {
        if (sheet == null) return;
        
        var sprite = Sprite.CreateStatic(tag, sheet, x, y, width, height);
        spriteCache[tag] = sprite;
        Console.WriteLine($"Registered sprite: {tag} at ({x},{y}).");
    }

    /// <summary>
    /// Get a sprite by tag.
    /// </summary>
    public static Sprite? GetSprite(string tag)
    {
        if (spriteCache.ContainsKey(tag))
            return spriteCache[tag];
        return null;
    }

    /// <summary>
    /// Check if sprites are available.
    /// </summary>
    public static bool HasSprites()
    {
        return spriteCache.Count > 0;
    }
}
