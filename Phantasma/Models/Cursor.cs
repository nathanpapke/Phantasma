using System;

namespace Phantasma.Models;

/// <summary>
/// UI Cursor for Selection
/// </summary>
public class Cursor : Object
{
    public override ObjectLayer Layer => ObjectLayer.Cursor;
    
    // Range and boundary properties
    private int range;
    private bool bounded;
    private int originX;
    private int originY;
    private bool active;
    
    public Cursor()
    {
        range = 0;
        bounded = false;
        originX = 0;
        originY = 0;
        active = false;
    }
    
    /// <summary>
    /// Initialize cursor with an object type (for sprite).
    /// </summary>
    public void Init(ObjectType type)
    {
        // Set object type for rendering.
        // TODO: Set sprite from type when sprite system is fully implemented.
    }
    
    /// <summary>
    /// Move the cursor by delta x/y.
    /// Returns whether the move was successful.
    /// </summary>
    public bool Move(int dx, int dy)
    {
        if (Position?.Place == null)
            return false;
        
        int newX = Position.X + dx;
        int newY = Position.Y + dy;
        
        // Wrap coordinates if map wraps.
        newX = Position.Place.WrapX(newX);
        newY = Position.Place.WrapY(newY);
        
        // Check if new location is off the map.
        if (Position.Place.IsOffMap(newX, newY))
            return false;
        
        // Check if viewport bounded and if new location is within viewport.
        if (bounded)
        {
            // TODO: Check if within viewport using Map.TileIsWithinViewport()
            // For now, allow movement.
        }
        
        // Check range from origin.
        int distance = CalculateDistance(originX, originY, newX, newY);
        if (distance > range)
            return false;
        
        // Move the cursor.
        Relocate(Position.Place, newX, newY);
        
        return true;
    }
    
    /// <summary>
    /// Set the maximum range from origin.
    /// </summary>
    public void SetRange(int newRange)
    {
        range = newRange;
    }
    
    /// <summary>
    /// Set the origin point for range calculations.
    /// </summary>
    public void SetOrigin(int x, int y)
    {
        originX = x;
        originY = y;
    }
    
    /// <summary>
    /// Set whether cursor is bounded to current viewport.
    /// </summary>
    public void SetViewportBounded(bool isBounded)
    {
        bounded = isBounded;
    }
    
    /// <summary>
    /// Check if cursor is currently active (visible).
    /// </summary>
    public bool IsActive()
    {
        return active;
    }
    
    /// <summary>
    /// Relocate cursor to a new position and activate it.
    /// </summary>
    public void Relocate(Place place, int x, int y)
    {
        SetPosition(place, x, y);
        active = true;
    }
    
    /// <summary>
    /// Remove cursor from display.
    /// </summary>
    public new void Remove()
    {
        active = false;
        base.Remove();
    }
    
    /// <summary>
    /// Calculate Chebyshev distance (max of dx, dy).
    /// </summary>
    private int CalculateDistance(int x1, int y1, int x2, int y2)
    {
        return Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1));
    }
}
