using System;
using IronScheme.Runtime;

namespace Phantasma.Models;

public partial class Kernel
{    
    /// <summary>
    /// (kern-blit-map dst dst-x dst-y src src-x src-y w h)
    /// Copy terrain from one map to another.
    /// </summary>
    /// <param name="dst"></param>
    /// <param name="dstX"></param>
    /// <param name="dstY"></param>
    /// <param name="src"></param>
    /// <param name="srcX"></param>
    /// <param name="srcY"></param>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <returns></returns>
    public static object BlitMap(object dst, object dstX, object dstY,
        object src, object srcX, object srcY,
        object w, object h)
    {
        var dstMap = ResolveObject<TerrainMap>(dst);
        var srcMap = ResolveObject<TerrainMap>(src);
        
        if (dstMap == null)
        {
            RuntimeError("kern-blit-map: null destination map");
            return Builtins.Unspecified;
        }
        
        if (srcMap == null)
        {
            RuntimeError("kern-blit-map: null source map");
            return Builtins.Unspecified;
        }
        
        int dx = ToInt(dstX, 0);
        int dy = ToInt(dstY, 0);
        int sx = ToInt(srcX, 0);
        int sy = ToInt(srcY, 0);
        int width = ToInt(w, srcMap.Width);
        int height = ToInt(h, srcMap.Height);
        
        // Clip dimensions to valid ranges
        width = Math.Min(width, Math.Min(dstMap.Width - dx, srcMap.Width - sx));
        height = Math.Min(height, Math.Min(dstMap.Height - dy, srcMap.Height - sy));
        
        if (width <= 0 || height <= 0)
        {
            Console.WriteLine($"[kern-blit-map] Nothing to blit (clipped to 0)");
            return dstMap;
        }
        
        Console.WriteLine($"[kern-blit-map] Blitting {width}x{height} from ({sx},{sy}) to ({dx},{dy})");
        
        // Copy terrain tiles
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var terrain = srcMap.GetTerrain(sx + x, sy + y);
                if (terrain != null)
                {
                    dstMap.SetTerrain(dx + x, dy + y, terrain);
                }
            }
        }
        
        return dstMap;
    }
    
    /// <summary>
    /// (kern-map-rotate map degrees)
    /// Rotate a terrain map.
    /// </summary>
    /// <param name="mapArg"></param>
    /// <param name="degrees"></param>
    /// <returns></returns>
    public static object MapRotate(object mapArg, object degrees)
    {
        var map = ResolveObject<TerrainMap>(mapArg);
        
        if (map == null)
        {
            RuntimeError("kern-map-rotate: null map");
            return Builtins.Unspecified;
        }
        
        int deg = ToInt(degrees, 0) % 360;
        if (deg < 0) deg += 360;
        
        // Normalize to 0, 90, 180, 270
        deg = (deg / 90) * 90;
        
        if (deg == 0)
            return map;
        
        Console.WriteLine($"[kern-map-rotate] Rotating map by {deg} degrees");
        
        // For 90/270 degrees, width and height swap
        int oldW = map.Width;
        int oldH = map.Height;
        int newW = (deg == 90 || deg == 270) ? oldH : oldW;
        int newH = (deg == 90 || deg == 270) ? oldW : oldH;
        
        var newTerrain = new Terrain[newW, newH];
        
        for (int y = 0; y < oldH; y++)
        {
            for (int x = 0; x < oldW; x++)
            {
                int nx, ny;
                switch (deg)
                {
                    case 90:
                        nx = oldH - 1 - y;
                        ny = x;
                        break;
                    case 180:
                        nx = oldW - 1 - x;
                        ny = oldH - 1 - y;
                        break;
                    case 270:
                        nx = y;
                        ny = oldW - 1 - x;
                        break;
                    default:
                        nx = x;
                        ny = y;
                        break;
                }
                
                newTerrain[nx, ny] = map.GetTerrain(x, y);
            }
        }
        
        // Update map dimensions and terrain.
        map.Rotate(newW, newH, newTerrain);
        
        return map;
    }
}
