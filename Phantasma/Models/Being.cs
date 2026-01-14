using System;
using System.Collections.Generic;

namespace Phantasma.Models;

/// <summary>
/// Base Class for All Living Entities (NPCs, monsters, player)
/// </summary>
public abstract class Being : Object
{
    public override ObjectLayer Layer => ObjectLayer.Being;
    
    private string name;
    private int baseFaction;
    private int currentFaction;
    public AStarNode CachedPath;
    public Place CachedPathPlace;
    private int visible = 1;
    
    // Cached Path for Multi-turn Movement
    protected LinkedList<AStarNode>? cachedPath;
    protected Place? cachedPathPlace;
    
    // Stats
    public int HP { get; set; }
    public int MaxHP { get; set; }
    public int MP { get; set; }
    public int MaxMP { get; set; }
    public int ActionPoints { get; set; }
    public int MaxActionPoints { get; set; }
    
    // Effects
    private bool isDead = false;
    public bool IsDead 
    { 
        get => isDead || HP <= 0;
        set => isDead = value;
    }
    public bool IsAsleep { get; protected set; } = false;
    public bool IsCharmed
    { get { return false; } }
    
    // Visual
    public Sprite CurrentSprite { get; set; }
    
    public Being() : base()
    {
        SetDefaults();
    }
    
    public void SetBaseFaction(int faction)
    {
        baseFaction = faction;
        currentFaction = faction;
    }

    public int GetBaseFaction()
    {
        return baseFaction;
    }

    public int GetCurrentFaction()
    {
        return currentFaction;
    }
    
    public Location GetCurrentPosition()
    {
        return Position;
    }

    public virtual bool Move(int dx, int dy)
    {
        if (Position.Place == null)
            return false;
        
        int newX = Position.X + dx;
        int newY = Position.Y + dy;
        
        // Check if new position is valid.
        if (Position.Place.IsOffMap(newX, newY))
            return false;
        
        // Check if terrain is passable using PassabilityTable.
        if (!Position.Place.IsPassable(newX, newY, this))
            return false;
        
        // Check if there's another being there.
        if (Position.Place.GetBeingAt(newX, newY) != null)
            return false;
        
        // Update position in place.
        Position.Place.MoveBeing(this, newX, newY);
        
        // Update our position.
        Position.X = newX;
        Position.Y = newY;
        
        return true;
    }

    public virtual bool PathFindTo(Place place, int destX, int destY, int flags = 0)
    {
        if (Position?.Place == null || Position.Place != place)
            return false;
    
        // Already there?
        if (Position.X == destX && Position.Y == destY)
            return true;
    
        // Invalidate cache if place changed.
        if (cachedPathPlace != place)
            cachedPath = null;
    
        // Find path if we don't have one.
        if (cachedPath == null)
        {
            cachedPath = AStar.Search(
                Position.X, Position.Y,
                destX, destY,
                place.Width, place.Height,
                (x, y) => place.IsPassable(x, y, this)
            );
            cachedPathPlace = place;
        }
    
        if (cachedPath == null || cachedPath.Count < 2)
            return false;
    
        // First node is current position, remove it.
        cachedPath.RemoveFirst();
    
        // Get next step.
        var next = cachedPath.First?.Value;
        if (next == null)
            return false;
    
        // Take one step.
        int dx = next.X - Position.X;
        int dy = next.Y - Position.Y;
    
        return Move(dx, dy);
    }
    
    /// <summary>
    /// Get path to destination, using cache if valid.
    /// </summary>
    protected virtual LinkedList<AStarNode>? GetPathTo(Place place, int destX, int destY)
    {
        // Invalidate cache if place changed.
        if (cachedPathPlace != place)
            cachedPath = null;
        
        // Compute fresh path.
        cachedPath = AStar.Search(
            Position.X, Position.Y,
            destX, destY,
            place.Width, place.Height,
            (x, y) => place.IsPassable(x, y, this)
        );
        cachedPathPlace = place;
        
        return cachedPath;
    }

    public void SetName(string newName)
    {
        name = newName;
        Name = newName;  // Also set Object.Name property for consistency.
    }
    
    public string GetName()
    {
        return name ?? "Unknown";
    }
    
    public void Damage(int amount)
    {
        HP = Math.Max(0, HP - amount);
    }
    
    /// <summary>
    /// Kill this being.  Override in derived classes for specific behavior.
    /// </summary>
    public virtual void Kill()
    {
        HP = 0;
        Remove();
    }
    
    public void Heal(int amount)
    {
        HP = Math.Min(MaxHP, HP + amount);
    }
    
    public int GetDefend()
    {
        return 10; // Base defense value
    }
    
    public int GetArmor()
    {
        return 0; // No armor yet
    }
    
    public int GetExperienceValue()
    {
        return 10; // Base XP value
    }
    
    public string GetWoundDescription()
    {
        float hpPercent = (float)HP / MaxHP;
        if (hpPercent > 0.75f) return "scratched";
        if (hpPercent > 0.5f) return "wounded";
        if (hpPercent > 0.25f) return "badly wounded";
        if (hpPercent > 0) return "nearly dead";
        return "dead";
    }
    
    /// <summary>
    /// Get vision radius. Override in Character.
    /// </summary>
    public virtual int GetVisionRadius()
    {
        return 5;  // Default for animals/simple beings
    }
    
    /// <summary>
    /// Check if this being is visible to others.
    /// </summary>
    public virtual bool IsVisible()
    {
        return visible > 0;
    }

    public void SetVisible(bool val)
    {
        if (val)
            visible++;
        else
            visible--;
    }

    /// <summary>
    /// Check if this being can wander to a location.
    /// Unlike pathfinding, wandering avoids hazardous terrain.
    /// </summary>
    public virtual bool CanWanderTo(int x, int y)
    {
        var place = GetPlace();
        if (place == null)
            return false;
    
        if (!place.IsPassable(x, y, this))
            return false;
    
        var terrain = place.GetTerrain(x, y);
        if (terrain != null && terrain.IsHazardous)
            return false;
    
        return true;
    }
    
    protected void SetDefaults()
    {
        name = "";
        CachedPath = null; // new AStarNode();
        CachedPathPlace = null;
        HP = MaxHP = 100;
        MP = MaxMP = 50;
        ActionPoints = MaxActionPoints = 10;
        baseFaction = 0;
        currentFaction = 0;
        CurrentSprite = new Sprite();
    }

    protected void SwitchPlaces(Being being)
    {
        if (being == null || being.Position.Place != Position.Place)
            return;
            
        int tempX = Position.X;
        int tempY = Position.Y;
        Position.X = being.Position.X;
        Position.Y = being.Position.Y;
        being.Position.X = tempX;
        being.Position.Y = tempY;
    }
    
    // =========================================================
    // NEW: Transition helper methods
    // =========================================================

    /// <summary>
    /// Check what kind of move this is - normal, subplace entry, or off-map.
    /// </summary>
    protected MoveResult CheckMoveTo(Place place, int newX, int newY, int dx, int dy)
    {
        // Check if moving off map.
        if (place.IsOffMap(newX, newY))
        {
            // If place has a parent, we can exit.
            if (place.Location?.Place != null)
                return MoveResult.OffMap;
            
            // If map wraps, adjust coordinates.
            if (place.Wraps)
            {
                // Coordinates will be wrapped in actual movement.
                return MoveResult.Ok;
            }
            
            // Can't go off edge of non-wrapping map with no parent.
            return MoveResult.Impassable;
        }
        
        // Check if stepping onto a subplace.
        var subplace = place.GetSubplace(newX, newY);
        if (subplace != null)
        {
            return MoveResult.EnterSubplace;
        }
        
        return MoveResult.Ok;
    }

    /// <summary>
    /// Enter a subplace from the wilderness. Supports all 8 directions + HERE.
    /// </summary>
    public MoveResult EnterSubplace(Place subplace, int dx, int dy)
    {
        // Convert movement vector to direction.
        int dir;
        if (dx == 0 && dy == 0)
        {
            // No direction - could prompt user or default to HERE.
            dir = Common.HERE;
        }
        else
        {
            dir = Common.DeltaToDirection(dx, dy);
        }
        
        // Use OPPOSITE direction for edge entrance lookup.
        // This way, entering from NORTH (walking south) looks up SOUTH entrance,
        // which in Nazghul's data is at the north edge of the map.
        int lookupDir = Common.OppositeDirection(dir);
        
        // Get entry coordinates for the opposite direction.
        if (!subplace.GetEdgeEntrance((Direction)lookupDir, out int newX, out int newY))
        {
            return MoveResult.NoDestination;
        }
        
        // Relocate to the subplace.
        Relocate(subplace, newX, newY);
        
        return MoveResult.Ok;
    }

    /// <summary>
    /// Exit current place and return to parent place.
    /// Player appears adjacent to the subplace in the direction they exited.
    /// </summary>
    /// <param name="dx">Exit direction X component (-1, 0, or 1)</param>
    /// <param name="dy">Exit direction Y component (-1, 0, or 1)</param>
    /// <returns>MoveResult indicating success or failure</returns>
    public MoveResult ExitToParentPlace(int dx, int dy)
    {
        Console.WriteLine($"[ExitToParentPlace] dx={dx}, dy={dy}");
        
        var currentPlace = GetPlace();
        if (currentPlace == null)
            return MoveResult.OffMap;
        
        var parentPlace = currentPlace.Location?.Place;
        if (parentPlace == null)
            return MoveResult.OffMap;
        
        int baseX = currentPlace.Location.X;
        int baseY = currentPlace.Location.Y;
        
        // Calculate exit position: adjacent tile in exit direction.
        int exitX = baseX + dx;
        int exitY = baseY + dy;
        
        // Handle wrapping if parent map wraps.
        if (parentPlace.Wraps)
        {
            exitX = parentPlace.WrapX(exitX);
            exitY = parentPlace.WrapY(exitY);
        }
        
        // Only check if we're off the map entirely - ignore passability!
        if (parentPlace.IsOffMap(exitX, exitY))
        {
            // Can't exit off the edge of the world map, fall back to town tile.
            exitX = baseX;
            exitY = baseY;
        }
        
        Console.WriteLine($"[ExitToParentPlace] Relocating from {currentPlace.Name} to ({exitX},{exitY}) on {parentPlace.Name}");
        
        currentPlace.Exit();
        Relocate(parentPlace, exitX, exitY);
        
        return MoveResult.Ok;
    }
}
