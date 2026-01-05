namespace Phantasma.Models;

/// <summary>
/// Terrain map containing the actual terrain grid for a place.
/// Created from glyph strings using a terrain palette.
/// </summary>
public class TerrainMap
{
    public string? Tag { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public Terrain[,] TerrainGrid { get; set; }
    
    public TerrainMap(string? tag, int width, int height)
    {
        Tag = tag;
        Width = width;
        Height = height;
        TerrainGrid = new Terrain[width, height];
    }
    
    /// <summary>
    /// Get terrain at a specific location.
    /// </summary>
    public Terrain? GetTerrain(int x, int y)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
        {
            return TerrainGrid[x, y];
        }
        return null;
    }
    
    /// <summary>
    /// Set terrain at a specific location.
    /// </summary>
    public void SetTerrain(int x, int y, Terrain terrain)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
        {
            TerrainGrid[x, y] = terrain;
        }
    }
    
    /// <summary>
    /// Replace the terrain grid with a rotated version.
    /// Called by kern-map-rotate after computing the rotated grid.
    /// </summary>
    public void Rotate(int newWidth, int newHeight, Terrain[,] newGrid)
    {
        Width = newWidth;
        Height = newHeight;
        TerrainGrid = newGrid;
    }
}