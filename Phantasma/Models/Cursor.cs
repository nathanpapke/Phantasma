using System;

namespace Phantasma.Models;

/// <summary>
/// Text Input Cursor Sprite
/// Used for the blinking cursor in the command window.
/// </summary>
public class Cursor
{
    /// <summary>
    /// The sprite to use for the text cursor.
    /// Typically a blinking underscore or vertical bar.
    /// </summary>
    public Sprite Sprite { get; set; }
    
    public Cursor()
    {
        // Default: no sprite (use hardcoded rendering)
        Sprite = null;
    }
    
    /// <summary>
    /// Initialize with a sprite.
    /// </summary>
    public void SetSprite(Sprite sprite)
    {
        Sprite = sprite;
    }
}
