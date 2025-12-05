using System.Collections.Generic;

namespace Phantasma.Models;

/// <summary>
/// Vehicles (boats, horses)
/// </summary>
public class Vehicle : Object
{
    public override ObjectLayer Layer => ObjectLayer.Vehicle;
    
    // Vehicle-specific Properties
    
    /// <summary>
    /// The movement mode this vehicle provides.
    /// Determines what terrain the vehicle can traverse.
    /// </summary>
    public MovementMode MovementMode { get; set; }
    
    /// <summary>
    /// How many passengers can ride this vehicle.
    /// </summary>
    public int Capacity { get; set; }
    
    /// <summary>
    /// Current passengers aboard this vehicle.
    /// </summary>
    public List<Being> Passengers { get; set; } = new List<Being>();
    
    /// <summary>
    /// Convenience property to get the movement mode index for passability lookups.
    /// </summary>
    public int MovementModeIndex => MovementMode.Index;
}
