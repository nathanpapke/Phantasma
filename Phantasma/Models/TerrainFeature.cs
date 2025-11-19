namespace Phantasma.Models;

/// <summary>
/// Terrain Features (bridges, built-in doors)
/// </summary>
public class TerrainFeature : Object
{
    public override ObjectLayer Layer => ObjectLayer.TerrainFeature;
    
    // Feature-specific Properties
    public bool IsPassable { get; set; } = true;
}
