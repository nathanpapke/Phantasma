namespace Phantasma.Models;

/// <summary>
/// UI frame sprites for window borders.
/// </summary>
public class FrameSprites
{
    public Sprite ULC { get; set; }  // Upper-left corner
    public Sprite URC { get; set; }  // Upper-right corner
    public Sprite LLC { get; set; }  // Lower-left corner
    public Sprite LRC { get; set; }  // Lower-right corner
    public Sprite TD { get; set; }   // T-junction down
    public Sprite TU { get; set; }   // T-junction up
    public Sprite TL { get; set; }   // T-junction left
    public Sprite TR { get; set; }   // T-junction right
    public Sprite TX { get; set; }   // Cross junction
    public Sprite Horz { get; set; } // Horizontal edge
    public Sprite Vert { get; set; } // Vertical edge
    public Sprite EndL { get; set; } // End left
    public Sprite EndR { get; set; } // End right
}
