using System;
using System.Collections.Generic;

namespace Phantasma.Models;

/// <summary>
/// Vehicle - An instance of a vehicle that can be boarded and ridden.
/// 
/// Key behaviors:
/// - Can have an occupant (Party or Being)
/// - When occupied, position delegates to occupant
/// - Can be damaged and destroyed
/// - Handles wind-affected movement for ships
/// </summary>
public class Vehicle : Object
{
    public override ObjectLayer Layer => ObjectLayer.Vehicle;
    
    // ===================================================================
    // INSTANCE PROPERTIES
    // ===================================================================
    
    /// <summary>
    /// The VehicleType defining this vehicle's characteristics.
    /// </summary>
    public new VehicleType? Type { get; set; }
    
    /// <summary>
    /// Current direction the vehicle is facing.
    /// Uses Common direction constants (NORTH, SOUTH, etc.)
    /// </summary>
    public int Facing { get; set; } = Common.NORTH;
    
    /// <summary>
    /// Current hit points.
    /// </summary>
    public int Hp { get; set; }
    
    /// <summary>
    /// The Being/Party currently in this vehicle.
    /// Null if unoccupied.
    /// </summary>
    public Object? Occupant { get; set; }
    
    // ===================================================================
    // CONSTRUCTORS
    // ===================================================================
    
    public Vehicle() : base()
    {
    }
    
    /// <summary>
    /// Constructor used by VehicleType.CreateInstance()
    /// </summary>
    public Vehicle(VehicleType type) : base()
    {
        Type = type;
        base.Type = type;
        Facing = Common.NORTH;
        Hp = type.MaxHp;
    }
    
    /// <summary>
    /// Constructor used by kern-mk-vehicle
    /// </summary>
    public Vehicle(VehicleType type, int facing, int hp) : base()
    {
        Type = type;
        base.Type = type;
        Occupant = null;
        Facing = facing;
        Hp = hp;
    }
    
    // ===================================================================
    // POSITION DELEGATION
    // When occupied, position comes from occupant.
    // ===================================================================
    
    public override int GetX()
    {
        if (Occupant != null)
            return Occupant.GetX();
        return base.GetX();
    }
    
    public override int GetY()
    {
        if (Occupant != null)
            return Occupant.GetY();
        return base.GetY();
    }
    
    public override Place? GetPlace()
    {
        if (Occupant != null)
            return Occupant.GetPlace();
        return base.GetPlace();
    }
    
    // ===================================================================
    // TYPE DELEGATION
    // ===================================================================
    
    public new VehicleType? GetObjectType()
    {
        return Type;
    }
    
    public string GetName()
    {
        return Type?.Name ?? "vehicle";
    }
    
    public Sprite? GetSprite()
    {
        return Type?.Sprite;
    }
    
    public ArmsType? GetOrdnance()
    {
        return Type?.Ordnance;
    }
    
    public MovementMode? GetMovementMode()
    {
        return Type?.MovementMode;
    }
    
    public string GetMovementDescription()
    {
        return Type?.MovementDescription ?? "rides";
    }
    
    public object? GetMovementSound()
    {
        return Type?.MovementSound;
    }
    
    public bool MustTurn()
    {
        return Type?.MustTurn ?? false;
    }
    
    public bool IsVulnerable()
    {
        return Type?.IsVulnerable ?? false;
    }
    
    public object? GetFormation()
    {
        return Type?.Formation;
    }
    
    public int GetMaxHp()
    {
        return Type?.MaxHp ?? 0;
    }
    
    public int GetSpeed()
    {
        return Type?.Speed ?? 1;
    }
    
    // ===================================================================
    // FACING AND TURNING
    // ===================================================================
    
    /// <summary>
    /// Set the vehicle's facing direction.
    /// Returns false if the vehicle's sprite can't face that direction.
    /// </summary>
    public bool SetFacing(int direction)
    {
        if (Type != null && !Type.CanFace(direction))
            return false;
        
        Facing = direction;
        return true;
    }
    
    /// <summary>
    /// Turn the vehicle to face a movement direction.
    /// Returns true if vehicle turned, false if already facing that way.
    /// </summary>
    /// <param name="dx">X movement delta</param>
    /// <param name="dy">Y movement delta</param>
    /// <param name="cost">Output: action point cost of turn</param>
    public bool Turn(int dx, int dy, out int cost)
    {
        cost = 0;
    
        // Get the 8-direction result.
        int targetDirection = Common.DeltaToDirection(dx, dy);
    
        // Let the type constrain to supported facings.
        int targetFacing = Type?.GetNearestSupportedFacing(targetDirection) ?? targetDirection;
    
        // Already facing that direction.
        if (Facing == targetFacing)
            return false;
    
        // Set new facing.
        if (!SetFacing(targetFacing))
            return false;
    
        cost = GetSpeed();
        return true;
    }
    
    /// <summary>
    /// Get the movement cost multiplier based on wind.
    /// </summary>
    public int GetMovementCostMultiplier()
    {
        // Get current wind direction from session/world.
        int windDirection = GetWindDirection();
        return Type?.GetWindPenalty(Facing, windDirection) ?? 1;
    }
    
    /// <summary>
    /// Get the current wind direction.
    /// </summary>
    private int GetWindDirection()
    {
        return Phantasma.MainSession?.Wind?.Direction ?? Common.NORTH;
    }
    
    // ===================================================================
    // WEAPON FIRING (for ships with cannons)
    // ===================================================================
    
    /// <summary>
    /// Get the facing required to fire weapon in a direction.
    /// Ships fire broadsides (perpendicular to facing).
    /// </summary>
    public int GetFacingToFireWeapon(int dx, int dy)
    {
        if (dx != 0)
            return Common.NORTH;
        if (dy != 0)
            return Common.EAST;
        return -1;
    }
    
    /// <summary>
    /// Check if the vehicle can fire in the given direction based on its firing mode.
    /// </summary>
    /// <param name="dx">Direction X (-1, 0, 1)</param>
    /// <param name="dy">Direction Y (-1, 0, 1)</param>
    /// <returns>True if can fire in that direction</returns>
    public bool CanFireInDirection(int dx, int dy)
    {
        if (dx == 0 && dy == 0)
            return false;  // Can't fire at self
        
        var mode = Type.FiringMode;
        
        if (mode == FiringMode.Turret)
            return true;  // Can fire any direction
        
        int fireDir = Common.DeltaToDirection(dx, dy);
        int vdir = Facing;
        int opposite = Common.OppositeDirection(vdir);
        
        switch (mode)
        {
            case FiringMode.Broadside:
                // Perpendicular to facing: if facing N/S, fire E/W and vice versa.
                if (vdir == Common.NORTH || vdir == Common.SOUTH)
                    return dy == 0 && dx != 0;
                if (vdir == Common.EAST || vdir == Common.WEST)
                    return dx == 0 && dy != 0;
                return false;
                
            case FiringMode.Forward:
                // Must fire in same direction as facing.
                return fireDir == vdir;
                
            case FiringMode.ForwardArc:
                // Forward direction plus adjacent diagonals.
                return IsInForwardArc(vdir, fireDir);
                
            case FiringMode.Rear:
                // Must fire opposite to facing.
                return fireDir == opposite;
                
            default:
                return false;
        }
    }
    
    /// <summary>
    /// Check if a firing direction is within the forward arc of a facing.
    /// Forward arc includes the facing direction and the two adjacent diagonals.
    /// </summary>
    private bool IsInForwardArc(int facing, int fireDir)
    {
        // Forward arc is facing direction +/- 1 in the direction enum.
        // NW=0, N=1, NE=2, W=3, HERE=4, E=5, SW=6, S=7, SE=8
        
        if (fireDir == facing)
            return true;
        
        // Get adjacent directions based on facing.
        return facing switch
        {
            Common.NORTH => fireDir == Common.NORTHWEST || fireDir == Common.NORTHEAST,
            Common.SOUTH => fireDir == Common.SOUTHWEST || fireDir == Common.SOUTHEAST,
            Common.EAST => fireDir == Common.NORTHEAST || fireDir == Common.SOUTHEAST,
            Common.WEST => fireDir == Common.NORTHWEST || fireDir == Common.SOUTHWEST,
            Common.NORTHWEST => fireDir == Common.NORTH || fireDir == Common.WEST,
            Common.NORTHEAST => fireDir == Common.NORTH || fireDir == Common.EAST,
            Common.SOUTHWEST => fireDir == Common.SOUTH || fireDir == Common.WEST,
            Common.SOUTHEAST => fireDir == Common.SOUTH || fireDir == Common.EAST,
            _ => false
        };
    }
    
    /// <summary>
    /// Fire the vehicle's weapon in a direction.
    /// </summary>
    /// <param name="dx">Direction X (-1, 0, 1)</param>
    /// <param name="dy">Direction Y (-1, 0, 1)</param>
    /// <param name="user">Object firing the weapon (for AP cost)</param>
    /// <returns>True if fired successfully, false if invalid direction</returns>
    public bool FireWeapon(int dx, int dy, Object user)
    {
        var ordnance = GetOrdnance();
        if (ordnance == null)
            return false;
    
        if (!CanFireInDirection(dx, dy))
            return false;
    
        ordnance.FireInDirection(GetPlace(), GetX(), GetY(), dx, dy, user);
        return true;
    }

    /// <summary>
    /// Get a description of why firing failed (for user feedback).
    /// </summary>
    public string GetFiringRestrictionMessage()
    {
        return Type.FiringMode switch
        {
            FiringMode.Broadside => "Not a broadside!",
            FiringMode.Forward => "Can only fire forward!",
            FiringMode.ForwardArc => "Target not in forward arc!",
            FiringMode.Rear => "Can only fire aft!",
            _ => "Cannot fire in that direction!"
        };
    }
    
    // ===================================================================
    // DAMAGE AND DESTRUCTION
    // ===================================================================
    
    /// <summary>
    /// Apply damage to the vehicle.
    /// </summary>
    public void Damage(int amount)
    {
        Hp -= amount;
        Hp = Math.Max(0, Hp);
        Hp = Math.Min(Hp, GetMaxHp());
        
        if (Hp == 0)
            Destroy();
    }
    
    /// <summary>
    /// Destroy the vehicle.
    /// If KillsOccupants is true, also destroys any occupant.
    /// </summary>
    public void Destroy()
    {
        base.Remove();
        
        if (Occupant != null && (Type?.KillsOccupants ?? false))
        {
            // Destroy occupant.
            if (Occupant is Being being)
                being.Kill();
            Occupant = null;
        }
    }
    
    // ===================================================================
    // SAVING
    // ===================================================================
    
    /// <summary>
    /// Generate Scheme code to recreate this vehicle.
    /// </summary>
    public string ToScheme()
    {
        return $"(kern-mk-vehicle {Type?.Tag ?? "nil"} {Facing} {Hp})";
    }
}
