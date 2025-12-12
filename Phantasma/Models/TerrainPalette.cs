using System.Collections.Generic;

namespace Phantasma.Models;

/// <summary>
/// Terrain palette maps single-character glyphs to terrain types.
/// Used by TerrainMap to decode map layout strings.
/// </summary>
public class TerrainPalette
{
    public string Tag { get; set; }
    private Dictionary<string, Terrain> glyphToTerrain;
    
    public TerrainPalette(string tag)
    {
        Tag = tag;
        glyphToTerrain = new Dictionary<string, Terrain>();
    }
    
    /// <summary>
    /// Add a glyph->terrain mapping to this palette.
    /// </summary>
    public void AddMapping(string glyph, Terrain terrain)
    {
        if (terrain != null)
        {
            glyphToTerrain[glyph] = terrain;
        }
    }
    
    /// <summary>
    /// Get the terrain type for a given glyph.
    /// Returns null if glyph not found in palette.
    /// </summary>
    public Terrain? GetTerrainForGlyph(string glyph)
    {
        return glyphToTerrain.TryGetValue(glyph, out var terrain) ? terrain : null;
    }
}