using System;

namespace Phantasma.Models;

public class LineOfSight
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int Radius { get; set; }
    
    // Alpha = terrain transparency (input)
    // 0 = opaque (blocks vision), 12 = transparent
    public byte[] Alpha { get; private set; }
    
    // VMask = visibility result (output)
    // 0 = not visible, 1 = visible
    public byte[] VisibilityMask { get; private set; }
    
    public LineOfSight(int width, int height, int radius = 0)
    {
        Width = width;
        Height = height;
        Radius = radius;
        Alpha = new byte[width * height];
        VisibilityMask = new byte[width * height];
    }
    
    // Compute visibility using shadowcasting.
    public void Compute()
    {
        // Clear previous visibility.
        Array.Clear(VisibilityMask, 0, VisibilityMask.Length);
        
        // Center is always visible.
        int center = (Height / 2) * Width + (Width / 2);
        VisibilityMask[center] = 1;
        
        // Cast shadows in 8 octants.
        for (int octant = 0; octant < 8; octant++)
        {
            CastLight(1, 1.0, 0.0, octant);
        }
    }
    
    /// <summary>
    /// Recursively casts light in a single octant using shadowcasting.
    /// </summary>
    /// <param name="row">Current row being processed</param>
    /// <param name="start">Starting slope</param>
    /// <param name="end">Ending slope</param>
    /// <param name="octant">Which octant (0-7) to process</param>
    private void CastLight(int row, double start, double end, int octant)
    {
        if (start < end) return;
        
        int radius = Radius > 0 ? Radius : Math.Max(Width, Height) / 2;
        
        for (int j = row; j <= radius; j++)
        {
            double newStart = 0.0;
            bool blocked = false;
            
            for (int dx = -j, dy = -j; dx <= 0; dx++)
            {
                double leftSlope = (dx - 0.5) / (dy + 0.5);
                double rightSlope = (dx + 0.5) / (dy - 0.5);
                
                if (start < rightSlope) continue;
                if (end > leftSlope) break;

                int x, y;
                (x, y) = TransformOctant(dx, dy, octant);
                int centerX = Width / 2;
                int centerY = Height / 2;
                int mapX = centerX + x;
                int mapY = centerY + y;
                
                if (mapX < 0 || mapX >= Width || mapY < 0 || mapY >= Height)
                    continue;
                
                int index = mapY * Width + mapX;
                
                // Mark as visible.
                VisibilityMask[index] = 1;
                
                // Check if this tile blocks vision.
                if (Alpha[index] == 0) // Opaque
                {
                    if (!blocked)
                    {
                        blocked = true;
                        newStart = rightSlope;
                    }
                }
                else if (blocked)
                {
                    blocked = false;
                    start = newStart;
                }
            }
            
            if (blocked) break;
        }
    }

    /// <summary>
    /// Transforms coordinates from octant space to map space.
    /// This allows the same algorithm to work in all 8 directions.
    /// </summary>
    /// <param name="dx">Delta X in octant space</param>
    /// <param name="dy">Delta Y in octant space</param>
    /// <param name="octant">Which octant (0-7)</param>
    /// <returns>Transformed coordinates</returns>
    private static (int x, int y) TransformOctant(int dx, int dy, int octant)
    {
        return octant switch
        {
            0 => (dx, dy),
            1 => (dy, dx),
            2 => (dy, -dx),
            3 => (dx, -dy),
            4 => (-dx, -dy),
            5 => (-dy, -dx),
            6 => (-dy, dx),
            7 => (-dx, dy),
            _ => (0, 0)
        };
    }
}