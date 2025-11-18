using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using System.IO;

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
    public static Bitmap LoadImage(string filename)
    {
        if (imageCache.ContainsKey(filename))
            return imageCache[filename];
            
        try
        {
            // Try multiple paths.
            string[] searchPaths = 
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sprites", filename),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", filename),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename),
                filename
            };
            
            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    var bitmap = new Bitmap(path);
                    imageCache[filename] = bitmap;
                    Console.WriteLine($"Loaded sprite sheet: {path}.");
                    return bitmap;
                }
            }
            
            Console.WriteLine($"Sprite sheet not found: {filename}.");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading sprite sheet {filename}: {ex.Message}");
            return null;
        }
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
