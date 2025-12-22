using Avalonia.Media.Imaging;

namespace Phantasma.Models;

/// <summary>
/// Sprite - animation sequence with different facings (from Nazghul)
/// </summary>
public class Sprite
{
    public string Tag { get; set; }             // Script variable name for the sprite
    public int NFrames { get; set; }            // per sequence (1 for static)
    public int NTotalFrames { get; set; }       // n_frames x # facings
    public int Facing { get; set; }             // current facing sequence
    public int Facings { get; set; }            // bitmap of supported facing sequences
    public int Sequence { get; private set; }   // current animation sequence
    public string Decor { get; set; }           // decoration sprites
    public int WPix { get; set; }               // frame width in pixels
    public int HPix { get; set; }               // frame height in pixels
    public int Faded { get; set; }              // render sprite semi-transparent
    public int Wave { get; set; }               // vertical roll effect
    
    public Bitmap SourceImage { get; set; }     // The loaded image
    public int SourceX { get; set; }            // X position in sprite sheet
    public int SourceY { get; set; }            // Y position in sprite sheet
    
    // ASCII Fallback Character
    public char DisplayChar { get; set; }

    public Sprite()
    {
        NFrames = 1;
        NTotalFrames = 1;
        WPix = 32;  // Default tile size
        HPix = 32;
    }
    
    // Constructor for Scheme/Kernel (kern-mk-sprite)
    public Sprite(string filename)
    {
        Tag = filename;
        NFrames = 1;
        NTotalFrames = 1;
        WPix = 32;  // Default tile size
        HPix = 32;
        // TODO: Load actual image from filename
        // SourceImage = LoadImage(filename);
    }
    
    // Constructor for Scheme/Kernel (kern-mk-sprite) with sprite sheet position
    public Sprite(string filename, int x, int y, int width = 32, int height = 32)
    {
        Tag = filename;
        NFrames = 1;
        NTotalFrames = 1;
        WPix = width;
        HPix = height;
        SourceX = x;
        SourceY = y;
        // TODO: Load actual image from filename
        // SourceImage = SpriteManager.LoadImage(filename);
    }
        
    /// <summary>
    /// Create a simple static sprite.
    /// </summary>
    public static Sprite CreateStatic(string tag, Bitmap image, int x, int y, int width, int height)
    {
        return new Sprite
        {
            Tag = tag,
            NFrames = 1,
            NTotalFrames = 1,
            WPix = width,
            HPix = height,
            SourceImage = image,
            SourceX = x,
            SourceY = y
        };
    }

    /// <summary>
    /// Set the facing direction and compute the sequence index.
    /// Returns false if the facing is not supported.
    /// </summary>
    public bool SetFacing(int direction)
    {
        if (Facings != 0 && (Facings & (1 << direction)) == 0)
            return false;  // Direction not supported
    
        Facing = direction;
    
        // Count set bits before this direction.
        Sequence = 0;
        for (int i = 0; i < direction; i++)
        {
            if ((Facings & (1 << i)) != 0)
                Sequence++;
        }
    
        return true;
    }
}
