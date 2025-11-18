namespace Phantasma.Models;

/// <summary>
/// Object layers for multi-layer object system.
/// </summary>
public enum ObjectLayer
{
    TerrainFeature = 0,  // Bridges, doors in terrain
    Field = 1,           // Fire, poison fields
    Mechanism = 2,       // Levers, buttons, doors
    Object = 3,          // Generic objects, items on ground
    Container = 4,       // Chests, barrels
    Being = 5            // Characters, NPCs, creatures
}
