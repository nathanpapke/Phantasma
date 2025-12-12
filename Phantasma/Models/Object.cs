using System;

namespace Phantasma.Models;

/// <summary>
/// Base Class for All Game Objects (items, beings, etc.)
/// </summary>
public abstract class Object
{
    public abstract ObjectLayer Layer { get; } //removed set;
    
    private static int nextId = 1;
    
    public int Id { get; protected set; }
    public ObjectType Type { get; set; }
    public string Name { get; set; }
    public Location Position { get; set; }
    public int Count { get; set; } = 1;
    public Gob Gob { get; set; }  // Scheme object reference
    
    public Object()
    {
        Id = nextId++;
        Position = new Location(null, 0, 0);
    }
    
    public virtual bool Use(Being user)
    {
        return false;
    }
    
    public virtual bool Ready(Character character)
    {
        return false;
    }
    
    public Location GetPosition()
    {
        return Position;
    }
    
    public void SetPosition(Location loc)
    {
        Position = loc;
    }
    
    public void SetPosition(Place place, int x, int y)
    {
        Position = new Location(place, x, y);
    }
    
    public int GetX()
    {
        return Position.X;
    }
    
    public int GetY()
    {
        return Position.Y;
    }
    
    public Place GetPlace()
    {
        return Position.Place;
    }
    
    public bool IsOnMap()
    {
        return Position.Place != null && 
               !Position.Place.IsOffMap(Position.X, Position.Y);
    }
    
    public bool IsItem()
    {
        return Layer == ObjectLayer.Item;
    }
    
    public bool IsContainer()
    {
        return Layer == ObjectLayer.Container;
    }
    
    public bool IsGettable()
    {
        return Layer == ObjectLayer.Item;
    }
    
    public bool CanHandle()
    {
        return Layer == ObjectLayer.Mechanism;
    }
    
    public virtual void Handle(Character user)
    {
        // Override in mechanisms.
    }
    
    public virtual void Get(Character getter)
    {
        // Override in items.
    }

    public virtual Object? GetSpeaker()
    {
        // Override in party.
        
        return null;
    }
    
    public virtual void Remove()
    {
        if (Position.Place != null)
        {
            Position.Place.RemoveObject(this);
        }
    }
}