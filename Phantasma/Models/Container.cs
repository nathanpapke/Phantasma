using System.Collections.Generic;
using System.Linq;

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
    private object? trap;  // Closure for trap effect
        
    public Container() : base()
    {
        Contents = new List<Item>();
        isOpen = false;
        isTrapped = false;
    }
    
    public Container(ObjectType? type) : this()
    {
        if (type != null)
            Type = type;
    }
        
    public bool IsTrapped()
    {
        return isTrapped;
    }
        
    public object? GetTrap()
    {
        return null; // Stub for now
    }
    
    public void SetTrap(object? trapClosure)
    {
        trap = trapClosure;
        isTrapped = trap != null;
    }
        
    public void Open()
    {
        isOpen = true;
    }
    
    public bool IsEmpty()
    {
        return Contents.Count == 0;
    }
    
    /// <summary>
    /// Search for an item type in the container.
    /// Returns the Item if found, null otherwise.
    /// </summary>
    public Item? Search(ObjectType? type)
    {
        if (type == null)
            return null;
        
        return Contents.FirstOrDefault(item => 
            item.Type == type || item.Type?.Tag == type.Tag);
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
    
    /// <summary>
    /// Remove items of a type from the container.
    /// Returns true if successful, false if not enough items.
    /// </summary>
    public bool RemoveItem(ObjectType? type, int quantity = 1)
    {
        if (type == null || quantity <= 0)
            return false;
        
        var item = Search(type);
        if (item == null)
            return false;
        
        if (item.Quantity < quantity)
            return false;  // Not enough
        
        item.Quantity -= quantity;
        
        // Remove item if empty
        if (item.Quantity <= 0)
        {
            Contents.Remove(item);
        }
        
        return true;
    }
        
    // Stub methods for Character.cs
    public Object? GetReadyableItems()
    {
        return null; // Will implement later
    }
}
