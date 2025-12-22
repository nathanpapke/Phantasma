using System;

namespace Phantasma.Models;

/// <summary>
/// Base Class for All Living Entities (NPCs, monsters, player)
/// </summary>
public abstract class Being : Object
{
    public override ObjectLayer Layer => ObjectLayer.Being;
    
    public AStarNode CachedPath;
    public Place CachedPathPlace;
    private string name;
    private int baseFaction;
    private int currentFaction;
    
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
            
        // Check if terrain is passable.
        var terrain = Position.Place.GetTerrain(newX, newY);
        if (terrain != null && !terrain.IsPassable)
            return false;
            
        // Check if there's another being there
        if (Position.Place.GetBeingAt(newX, newY) != null)
            return false;
        
        // Update position in place.
        Position.Place.MoveBeing(this, newX, newY);
        
        // Update our position.
        Position.X = newX;
        Position.Y = newY;
        
        return true;
    }

    public bool PathFindTo(Place place, int x, int y, int flags = 0)
    {
        // Pathfinding
        return false;
    }

    public void SetName(string newName)
    {
        name = newName;
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

    protected void SetDefaults()
    {
        name = "";
        CachedPath = new AStarNode();
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
            if (place.Location.Place != null)
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
    /// Enter a subplace from the parent map.
    /// </summary>
    protected bool EnterSubplace(Place subplace, int dx, int dy)
    {
        int entryDir = Common.DeltaToDirection(-dx, -dy);
    
        if (!subplace.GetEdgeEntrance((Direction)entryDir, out int entryX, out int entryY))
        {
            entryX = subplace.Width / 2;
            entryY = subplace.Height / 2;
        }
    
        Console.WriteLine($"{GetName()} enters {subplace.Name}");
        Relocate(subplace, entryX, entryY);
        return true;
    }

    /// <summary>
    /// Exit to parent place when walking off map edge.
    /// </summary>
    protected bool ExitToParentPlace(int dx, int dy)
    {
        var currentPlace = Position?.Place;
        var parentPlace = currentPlace?.Location.Place;
    
        if (parentPlace == null)
        {
            Console.WriteLine($"{GetName()} can't leave - no parent place!");
            return false;
        }
    
        int parentX = currentPlace.Location.X;
        int parentY = currentPlace.Location.Y;
    
        Console.WriteLine($"{GetName()} exits {currentPlace.Name} to {parentPlace.Name}");
        Relocate(parentPlace, parentX, parentY);
        return true;
    }
}
