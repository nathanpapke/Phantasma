using System;

namespace Phantasma.Models;

/// <summary>
/// Base Class for All Game Objects (items, beings, etc.)
/// </summary>
public class Object
{
    private static int nextId = 1;
    
    public int Id { get; protected set; }
    public ObjectType Type { get; set; }
    public string Name { get; set; }
    public Location Position { get; set; }
    public int Count { get; set; } = 1;
    public Gob Gob { get; set; }  // Scheme object reference
    
    // Object Layers (Determines draw order and interaction.)
    public enum Layer
    {
        Terrain = 0,
        Item = 1,
        Container = 2,
        Mechanism = 3,
        Being = 4,
        Effect = 5
    }
    
    public Layer ObjectLayer { get; set; }
    
    public Object()
    {
        Id = nextId++;
        Position = new Location(null, 0, 0);
    }
    
    public Object(ObjectType type) : this()
    {
        Type = type;
        if (type != null)
        {
            ObjectLayer = type.Layer;
        }
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
        Position.Place = place;
        Position.X = x;
        Position.Y = y;
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
        return ObjectLayer == Layer.Item;
    }
    
    public bool IsContainer()
    {
        return ObjectLayer == Layer.Container;
    }
    
    public bool IsGettable()
    {
        return ObjectLayer == Layer.Item;
    }
    
    public bool CanHandle()
    {
        return ObjectLayer == Layer.Mechanism;
    }
    
    public virtual void Handle(Character user)
    {
        // Override in mechanisms.
    }
    
    public virtual void Get(Character getter)
    {
        // Override in items.
    }
    
    public virtual void Remove()
    {
        if (Position.Place != null)
        {
            Position.Place.RemoveObject(this);
        }
    }
}