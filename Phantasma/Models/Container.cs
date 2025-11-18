using System.Collections.Generic;

namespace Phantasma.Models;

/// <summary>
/// Container for Holding Items (chests, bags, character inventory)
/// </summary>
public class Container : Object
{
    private List<Object> contents;
    private bool isOpen;
    private bool isTrapped;
        
    public Container() : base()
    {
        ObjectLayer = Layer.Container;
        contents = new List<Object>();
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
        
    public List<Object> GetContents()
    {
        return contents;
    }
        
    public void AddItem(Object item)
    {
        if (item != null)
            contents.Add(item);
    }
        
    public void RemoveItem(Object item)
    {
        contents.Remove(item);
    }
        
    // Stub methods for Character.cs
    public Object? GetReadyableItems()
    {
        return null; // Will implement later
    }
}