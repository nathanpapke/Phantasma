namespace Phantasma.Models;

/// <summary>
/// Defines types of objects in the game.
/// </summary>
public class ObjectType
{
    public string Tag { get; set; } = "";
    public string Name { get; set; } = "";
    public Sprite? Sprite { get; set; }
    public Object.Layer Layer { get; set; }  // Reference to Object.Layer enum
    public bool Passable { get; set; } = true;
    public bool Transparent { get; set; } = true;
    public int Weight { get; set; }
    public int Value { get; set; }
        
    // Behaviors
    public bool Gettable { get; set; }
    public bool Usable { get; set; }
    public bool Readyable { get; set; }
        
    public ObjectType()
    {
        Layer = Object.Layer.Item;  // Default to item layer
    }
        
    public ObjectType(string tag, string name, Object.Layer layer)
    {
        Tag = tag;
        Name = name;
        Layer = layer;
    }
        
    public virtual Object CreateInstance()
    {
        return new Object(this);
    }
        
    // Helper Methods
    public bool IsReadyable()
    {
        return Readyable;
    }
        
    public bool CanHandle()
    {
        return Layer == Object.Layer.Mechanism;
    }
        
    public void Get(Object item, Character getter)
    {
        // Implementation for Getting Items
        if (Gettable && getter != null)
        {
            item.Remove();
            // TODO: Add to getter's inventory
        }
    }
        
    public void Handle(Object obj, Character user)
    {
        // Implementation for Handling Mechanisms
        // Will be implemented when needed.
    }
}