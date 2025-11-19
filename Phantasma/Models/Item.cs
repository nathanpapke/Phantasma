namespace Phantasma.Models;

/// <summary>
/// Items that can be Picked Up
/// </summary>
public class Item : Object
{
    public override ObjectLayer Layer => ObjectLayer.Item;
    
    // Item-specific properties
    public int Weight { get; set; }
    public int Value { get; set; }
    public bool IsStackable { get; set; }
    public int Quantity { get; set; } = 1;
}
