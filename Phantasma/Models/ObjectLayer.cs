namespace Phantasma.Models;

/// <summary>
/// Object layers for rendering and interaction order.
/// </summary>
/// <remarks>
/// CRITICAL: Lower layer numbers render FIRST (on bottom).
/// Higher layer numbers render LAST (on top).
/// 
/// Rendering order (bottom to top):
///   Terrain (always first)
///   → TerrainFeature (bridges over water)
///   → Mechanism (doors, levers)
///   → Portal (stairs)
///   → Vehicle (boats, horses)
///   → Bed (for sleeping)
///   → Container (chests)
///   → Item (swords, potions on ground)
///   → Field (fire, poison)
///   → Being (characters - always visible on top)
///   → Missile (arrows in flight)
///   → Cursor (UI element - topmost)
/// 
/// Note from Nazghul: "Proper rendering depends on keeping these in order!"
/// </remarks>
public enum ObjectLayer
{
    /// <summary>
    /// Invalid/unused layer
    /// </summary>
    Null = 0,
        
    /// <summary>
    /// Terrain features like bridges, doors built into terrain
    /// Renders on top of terrain but under everything else.
    /// </summary>
    TerrainFeature = 1,
        
    /// <summary>
    /// Mechanisms: levers, buttons, switches, doors
    /// </summary>
    Mechanism = 2,
        
    /// <summary>
    /// Portals and transitions: stairs, ladders, exits
    /// </summary>
    Portal = 3,
        
    /// <summary>
    /// Vehicles: boats, horses, carts
    /// Characters can board these.
    /// </summary>
    Vehicle = 4,
        
    /// <summary>
    /// Beds for sleeping/resting
    /// </summary>
    Bed = 5,
        
    /// <summary>
    /// Containers: chests, barrels, crates
    /// Can hold items inside.
    /// </summary>
    Container = 6,
        
    /// <summary>
    /// Items on ground: weapons, armor, potions, gold
    /// Can be picked up.
    /// </summary>
    Item = 7,
        
    /// <summary>
    /// Fields: fire, poison gas, energy fields
    /// Environmental hazards.
    /// </summary>
    Field = 8,
        
    /// <summary>
    /// Beings: characters, NPCs, monsters
    /// Always render on top so they're visible.
    /// </summary>
    Being = 9,
        
    /// <summary>
    /// Missiles: arrows, spells in flight
    /// Temporary objects during combat.
    /// </summary>
    Missile = 10,
        
    /// <summary>
    /// UI cursor for selection
    /// Always on very top.
    /// </summary>
    Cursor = 11
}
