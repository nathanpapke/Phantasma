namespace Phantasma.Models;

/// <summary>
/// Terrain Features (bridges, built-in doors)
/// </summary>
public class TerrainFeature : Object
{
    public override ObjectLayer Layer => ObjectLayer.TerrainFeature;
    
    /// <summary>
    /// The ObjectType this feature was created from.
    /// </summary>
    //public ObjectType ObjectType => Type as ObjectType;
    
    /// <summary>
    /// Override sprite to use ObjectType's sprite if available.
    /// </summary>
    public override Sprite? Sprite => Type?.Sprite ?? base.Sprite;
    
    // Feature-specific Properties
    public bool IsPassable { get; set; } = true;
}
