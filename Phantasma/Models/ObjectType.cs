using System;

namespace Phantasma.Models;

/// <summary>
/// Defines types of objects in the game.
/// </summary>
public class ObjectType
{
    // ===================================================================
    // CAPABILITY FLAGS
    // These are bitmask values that can be combined.
    // ===================================================================
    
    public const int CAN_GET          = 1;
    public const int CAN_USE          = 2;
    public const int CAN_EXEC         = 4;
    public const int CAN_OPEN         = 8;
    public const int CAN_HANDLE       = 16;
    public const int CAN_STEP         = 32;
    public const int CAN_ATTACK       = 64;
    public const int CAN_MIX          = 128;
    public const int CAN_ENTER        = 256;
    public const int CAN_CAST         = 512;
    public const int CAN_BUMP         = 1024;
    public const int CAN_HIT_LOCATION = 2048;
    
    // ===================================================================
    // CORE PROPERTIES
    // ===================================================================
    
    public string Tag { get; set; } = "";
    public string Name { get; set; } = "";
    public Sprite? Sprite { get; set; }
    public ObjectLayer Layer { get; set; } = ObjectLayer.Item;
    
    /// <summary>
    /// Bitmask of capabilities (CAN_GET, CAN_USE, etc.)
    /// </summary>
    public int Capabilities { get; set; }
    
    /// <summary>
    /// Scheme closure for handling interactions.
    /// Called when player interacts with objects of this type.
    /// </summary>
    public object? InteractionHandler { get; set; }
    
    // Legacy properties (can be removed once all code uses Capabilities)
    public bool Passable { get; set; } = true;
    public bool Transparent { get; set; } = true;
    public int Weight { get; set; }
    public int Value { get; set; }
    
    // ===================================================================
    // COMPUTED CAPABILITY PROPERTIES
    // ===================================================================
    
    public bool CanGet => (Capabilities & CAN_GET) != 0;
    public bool CanUse => (Capabilities & CAN_USE) != 0;
    public bool CanExec => (Capabilities & CAN_EXEC) != 0;
    public bool CanOpen => (Capabilities & CAN_OPEN) != 0;
    public bool CanHandle => (Capabilities & CAN_HANDLE) != 0;
    public bool CanStep => (Capabilities & CAN_STEP) != 0;
    public bool CanAttack => (Capabilities & CAN_ATTACK) != 0;
    public bool CanMix => (Capabilities & CAN_MIX) != 0;
    public bool CanEnter => (Capabilities & CAN_ENTER) != 0;
    public bool CanCast => (Capabilities & CAN_CAST) != 0;
    public bool CanBump => (Capabilities & CAN_BUMP) != 0;
    public bool CanHitLocation => (Capabilities & CAN_HIT_LOCATION) != 0;
    
    // ===================================================================
    // CONSTRUCTORS
    // ===================================================================
    
    public ObjectType()
    {
    }
    
    public ObjectType(string tag, string name, ObjectLayer layer)
    {
        Tag = tag;
        Name = name;
        Layer = layer;
    }
    
    // ===================================================================
    // METHODS
    // ===================================================================
    
    /// <summary>
    /// Check if this type has a specific capability.
    /// </summary>
    public bool HasCapability(int capability)
    {
        return (Capabilities & capability) != 0;
    }
    
    /// <summary>
    /// Add a capability to this type.
    /// </summary>
    public void AddCapability(int capability)
    {
        Capabilities |= capability;
    }
    
    /// <summary>
    /// Remove a capability from this type.
    /// </summary>
    public void RemoveCapability(int capability)
    {
        Capabilities &= ~capability;
    }
}
