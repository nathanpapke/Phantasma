using System.Collections.Generic;

namespace Phantasma.Models;

/// <summary>
/// Vehicles (boats, horses)
/// </summary>
public class Vehicle : Object
{
    public override ObjectLayer Layer => ObjectLayer.Vehicle;
    
    // Vehicle-specific Properties
    public PassabilityTable.MovementMode MovementMode { get; set; }
    public int Capacity { get; set; } // How many can ride
    public List<Being> Passengers { get; set; } = new List<Being>();
}
