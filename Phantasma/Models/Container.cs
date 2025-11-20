using System.Collections.Generic;

namespace Phantasma.Models;

/// <summary>
/// Container for Holding Items (chests, bags, character inventory)
/// </summary>
public class Container : Object
{
    public override ObjectLayer Layer => ObjectLayer.Container;
    
    // Container-specific Properties
    public List<Item> Contents { get; set; } = new List<Item>();
    public int Capacity { get; set; }
    public bool IsLocked { get; set; }
    
    private bool isOpen;
    private bool isTrapped;
        
    public Container() : base()
    {
        Contents = new List<Item>();
        isOpen = false;
        isTrapped = false;
    }
        
    public bool IsTrapped()
    {
        return isTrapped;
    }
        
    public object? GetTrap()
    {
        return null; // Stub for now
    }
        
    public void Open()
    {
        isOpen = true;
    }
        
    public List<Item> GetContents()
    {
        return Contents;
    }
        
    public void AddItem(Item item)
    {
        if (item != null)
            Contents.Add(item);
    }
        
    public void RemoveItem(Item item)
    {
        Contents.Remove(item);
    }
        
    // Stub methods for Character.cs
    public Object? GetReadyableItems()
    {
        return null; // Will implement later
    }
}
