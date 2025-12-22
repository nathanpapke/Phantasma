using System;

namespace Phantasma.Models;

/// <summary>
/// VehicleType - Defines a type of vehicle (ship, horse, cart, etc.)
/// 
/// Matches Nazghul's VehicleType class from vehicle.h/vehicle.cpp.
/// Vehicles can:
/// - Carry passengers (occupants)
/// - Have mounted weapons (ordnance)
/// - Be affected by wind (ships)
/// - Provide alternative terrain passability via movement mode
/// </summary>
public class VehicleType : ObjectType
{
    /// <summary>
    /// Movement mode determining what terrain this vehicle can traverse.
    /// Ships can cross water, horses are faster on roads, etc.
    /// </summary>
    public MovementMode? MovementMode { get; set; }
    
    /// <summary>
    /// Terrain map used when combat occurs while in this vehicle.
    /// For ships, this would be a deck layout.
    /// </summary>
    public TerrainMap? CombatMap { get; set; }
    
    /// <summary>
    /// Formation defining where party members stand in combat.
    /// </summary>
    public object? Formation { get; set; }
    
    /// <summary>
    /// Mounted weapon type (cannons for ships, etc.)
    /// Null if vehicle has no weapon.
    /// </summary>
    public ArmsType? Ordnance { get; set; }
    
    // ===================================================================
    // DAMAGE AND DESTRUCTION
    // ===================================================================
    
    /// <summary>
    /// Maximum hit points for the vehicle.
    /// </summary>
    public int MaxHp { get; set; }
    
    /// <summary>
    /// Whether the vehicle can take damage in combat.
    /// </summary>
    public bool IsVulnerable { get; set; }
    
    /// <summary>
    /// Whether destroying the vehicle kills its occupants.
    /// </summary>
    public bool KillsOccupants { get; set; }
    
    // ===================================================================
    // MOVEMENT PROPERTIES
    // ===================================================================
    
    /// <summary>
    /// Base movement speed (action points cost per tile).
    /// </summary>
    public int Speed { get; set; }
    
    /// <summary>
    /// Whether the vehicle must turn to face direction of travel.
    /// Ships must turn; horses can move any direction.
    /// </summary>
    public bool MustTurn { get; set; }
    
    /// <summary>
    /// Movement description for UI ("sails", "rides", "walks").
    /// </summary>
    public string MovementDescription { get; set; } = "rides";
    
    /// <summary>
    /// Sound played when this vehicle moves.
    /// </summary>
    public Sound? MovementSound { get; set; }
    
    // ===================================================================
    // WIND PENALTIES (for sailing ships)
    // Speed multipliers based on wind direction:
    // - tailwind_penalty: sailing with the wind (fast)
    // - crosswind_penalty: sailing across the wind
    // - headwind_penalty: sailing against the wind (slow)
    // ===================================================================
    
    /// <summary>
    /// Speed multiplier when traveling with the wind.
    /// Lower = faster (e.g., 1 = normal speed).
    /// </summary>
    public int TailwindPenalty { get; set; } = 1;
    
    /// <summary>
    /// Speed multiplier when traveling across the wind.
    /// </summary>
    public int CrosswindPenalty { get; set; } = 1;
    
    /// <summary>
    /// Speed multiplier when traveling against the wind.
    /// Higher = slower (e.g., 4 = 4x slower).
    /// </summary>
    public int HeadwindPenalty { get; set; } = 1;
    
    /// <summary>
    /// How the vehicle's weapons can be aimed relative to facing.
    /// </summary>
    public FiringMode FiringMode { get; set; } = FiringMode.Broadside;
    
    // ===================================================================
    // CONSTRUCTORS
    // ===================================================================
    
    public VehicleType() : base()
    {
        Layer = ObjectLayer.Vehicle;
    }
    
    public VehicleType(
        string tag,
        string name,
        Sprite? sprite,
        TerrainMap? combatMap,
        ArmsType? ordnance,
        bool vulnerable,
        bool killsOccupants,
        bool mustTurn,
        string mvDesc,
        object? mvSound,
        int tailwindPenalty,
        int headwindPenalty,
        int crosswindPenalty,
        int maxHp,
        int speed,
        MovementMode? mmode) : base(tag, name, ObjectLayer.Vehicle)
    {
        this.Sprite = sprite;
        this.CombatMap = combatMap;
        this.Ordnance = ordnance;
        this.IsVulnerable = vulnerable;
        this.KillsOccupants = killsOccupants;
        this.MustTurn = mustTurn;
        this.MovementDescription = mvDesc ?? "rides";
        this.MovementSound = mvSound as Sound;
        this.TailwindPenalty = tailwindPenalty;
        this.HeadwindPenalty = headwindPenalty;
        this.CrosswindPenalty = crosswindPenalty;
        this.MaxHp = maxHp;
        this.Speed = speed;
        this.MovementMode = mmode;
    }
    
    // ===================================================================
    // METHODS
    // ===================================================================
    
    /// <summary>
    /// Check if the vehicle can face a particular direction.
    /// Based on sprite facings bitmask.
    /// </summary>
    public bool CanFace(int facing)
    {
        if (Sprite == null)
            return true; // No sprite = can face any direction
        
        int bit = 1 << facing;
        return (Sprite.Facings & bit) != 0;
    }
    
    /// <summary>
    /// Create a new Vehicle instance from this type.
    /// </summary>
    public Vehicle CreateInstance()
    {
        return new Vehicle(this);
    }
    
    /// <summary>
    /// Create a new Vehicle instance with specific facing and HP.
    /// Used by kern-mk-vehicle.
    /// </summary>
    public Vehicle CreateInstance(int facing, int hp)
    {
        return new Vehicle(this, facing, hp);
    }
    
    /// <summary>
    /// Calculate wind penalty based on vehicle facing vs wind direction.
    /// Uses dot product of facing vector and wind vector.
    /// </summary>
    /// <param name="vehicleFacing">Direction vehicle is facing</param>
    /// <param name="windDirection">Current wind direction</param>
    /// <returns>Speed multiplier (1 = normal, higher = slower)</returns>
    public int GetWindPenalty(int vehicleFacing, int windDirection)
    {
        // Non-turning vehicles (horses) ignore wind.
        if (!MustTurn)
            return 1;
        
        // Get direction vectors.
        int vdx = Common.DirectionToDx((Direction)vehicleFacing);
        int vdy = Common.DirectionToDy((Direction)vehicleFacing);
        int wdx = Common.DirectionToDx((Direction)windDirection);
        int wdy = Common.DirectionToDy((Direction)windDirection);
        
        // Calculate dot product.
        int dotProduct = vdx * wdx + vdy * wdy;
        
        return dotProduct switch
        {
            -1 => TailwindPenalty,   // With the wind
            0 => CrosswindPenalty,   // Across the wind
            1 => HeadwindPenalty,    // Against the wind
            _ => 1
        };
    }
    
    /// <summary>
    /// Get the nearest supported facing direction.
    /// If the sprite supports 8 directions, returns the exact direction.
    /// If sprite only has 4 directions, maps diagonals to cardinals.
    /// </summary>
    public int GetNearestSupportedFacing(int direction)
    {
        // If sprite supports this facing, use it directly.
        if (CanFace(direction))
            return direction;
        
        // Map diagonals to nearest cardinal (horizontal preference, matching Nazghul).
        return direction switch
        {
            Common.NORTHWEST or Common.SOUTHWEST => Common.WEST,
            Common.NORTHEAST or Common.SOUTHEAST => Common.EAST,
            _ => direction
        };
    }
}

/// <summary>
/// Firing modes for vehicle-mounted weapons.
/// </summary>
public enum FiringMode
{
    /// <summary>
    /// Perpendicular to facing only (sailing ships with broadside cannons).
    /// </summary>
    Broadside,
    
    /// <summary>
    /// Same direction as facing only (submarines, fighter craft).
    /// </summary>
    Forward,
    
    /// <summary>
    /// Forward arc - facing direction plus adjacent diagonals.
    /// </summary>
    ForwardArc,
    
    /// <summary>
    /// Opposite to facing only (rear-mounted weapons).
    /// </summary>
    Rear,
    
    /// <summary>
    /// Any direction (turret-mounted weapons, energy weapons).
    /// </summary>
    Turret
}
