using System;
using System.Collections.Generic;

namespace Phantasma.Models;

/// <summary>
/// Character Class for Player and NPCs
/// </summary>
public class Character : Being
{
    public Party Party { get; set; }
    public Species Species { get; set; }
    public Occupation Occ { get; set; }
    public bool IsClone { get; set; }
    public bool IsPlayer { get; set; }

    // Stats
    public int HitPoints { get; set; }
    public int MaxHitPoints { get; set; }
    public int HpMod { get; set; }
    public int HpMult { get; set; }
    public int MpMod { get; set; }
    public int MpMult { get; set; }
    public int Strength { get; set; }
    public int Intelligence { get; set; }
    public int Dexterity { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
    public int ArmourClass { get; set; }
    
    // Vision
    public int VisionRadius { get; set; } = 10;
    
    // Equipment Slots
    private List<ArmsType> readiedArms;
    private Container inventory;
    
    // Turn Management
    private bool turnEnded;
    
    public enum ReadyResult
    {
        Readied,
        NoAvailableSlot,
        WrongType,
        TooHeavy
    }

    public Character() : base()
    {
        readiedArms = new List<ArmsType>();
        Level = 1;
        Strength = 10;
        Intelligence = 10;
        Dexterity = 10;
        ArmourClass = 10;
    }

    public Character(string tag, string name,
        Sprite sprite,
        Species species, Occupation occupation,
        int str, int intl,
        int dex, int hpmod, int hpmult,
        int mpmod, int mpmult,
        int hp, int xp, int mp,
        int lvl) : this()
    {
        SetName(name);
        CurrentSprite = sprite;
        Species = species;
        Occ = occupation;
        Strength = str;
        Intelligence = intl;
        Dexterity = dex;
        HpMod = hpmod;
        HpMult = hpmult;
        MpMod = mpmod;
        MpMult = mpmult;
        HP = hp;
        Experience = xp;
        MP = mp;
        Level = lvl;
        
        // Calculate max values.
        MaxHP = HpMod + (HpMult * Level);
        MaxMP = MpMod + (MpMult * Level);
    }
    
    /// <summary>
    /// Create a simple test player character.
    /// </summary>
    public static Character CreateTestPlayer()
    {
        var player = new Character();
        player.SetName("Hero");
        player.IsPlayer = true;
        player.Level = 1;
        player.HP = player.MaxHP = 100;
        player.MP = player.MaxMP = 50;
        player.ActionPoints = player.MaxActionPoints = 10;
        player.ArmourClass = 10;
        
        // Try to load player sprite.
        var playerSprite = SpriteManager.GetSprite("player");
        if (playerSprite != null)
        {
            player.CurrentSprite = playerSprite;
        }
        else
        {
            // Create a placeholder sprite.
            player.CurrentSprite = new Sprite
            {
                Tag = "player",
                DisplayChar = '@',  // Classic roguelike player symbol
                SourceImage = null
            };
        }
        
        return player;
    }
    
    public bool IsTurnEnded()
    {
        return turnEnded || ActionPoints <= 0;
    }
    
    public void EndTurn()
    {
        turnEnded = true;
    }
    
    public void StartTurn()
    {
        turnEnded = false;
        ActionPoints = MaxActionPoints;
    }
    
    public void DecreaseActionPoints(int amount)
    {
        ActionPoints = Math.Max(0, ActionPoints - amount);
    }
    
    public int GetVisionRadius()
    {
        return VisionRadius;
    }
    
    public int GetDexterity()
    {
        return Dexterity;
    }
    
    public void AddExperience(int xp)
    {
        Experience += xp;
        CheckLevelUp();
    }
    
    private void CheckLevelUp()
    {
        int xpNeeded = Level * 100;
        if (Experience >= xpNeeded)
        {
            Level++;
            Experience -= xpNeeded;
            MaxHP = HpMod + (HpMult * Level);
            MaxMP = MpMod + (MpMult * Level);
            HP = MaxHP;
            MP = MaxMP;
            Console.WriteLine($"{GetName()} reached level {Level}!");
        }
    }
    
    public ReadyResult Ready(ArmsType weapon)
    {
        if (weapon == null)
            return ReadyResult.WrongType;
            
        // Simple ready - just add to list for now
        if (!readiedArms.Contains(weapon))
        {
            readiedArms.Add(weapon);
        }
        
        return ReadyResult.Readied;
    }
    
    public bool Unready(ArmsType weapon)
    {
        return readiedArms.Remove(weapon);
    }
    
    public bool HasReadied(ArmsType weapon)
    {
        return readiedArms.Contains(weapon);
    }
    
    public ArmsType EnumerateArms()
    {
        if (readiedArms.Count > 0)
            return readiedArms[0];
        return null;
    }
    
    public ArmsType GetNextArms()
    {
        // Simplified - just return null for now
        return null;
    }
    
    public bool HasAmmo(ArmsType weapon)
    {
        // Simplified - always have ammo for now
        return true;
    }
    
    public void UseAmmo(ArmsType weapon)
    {
        // Will implement when we add ammo tracking
    }
    
    public void TakeOut(ArmsType weapon, int count)
    {
        // Will implement with inventory
    }
    
    public void Add(ArmsType weapon, int count)
    {
        // Will implement with inventory
    }
    
    public Container GetInventoryContainer()
    {
        return inventory;
    }
    
    /// <summary>
    /// Attempt to move in a direction with collision detection.
    /// </summary>
    /// <param name="dx">X offset (-1, 0, or 1)</param>
    /// <param name="dy">Y offset (-1, 0, or 1)</param>
    /// <returns>True if movement succeeded, false if blocked.</returns>
    public override bool Move(int dx, int dy)
    {
        if (Position == null)
        {
            Console.WriteLine($"{Name} has no location!");
            return false;
        }
        
        // Calculate destination.
        int newX = Position.X + dx;
        int newY = Position.Y + dy;
        
        // Check if destination is passable.
        if (!Position.Place.IsPassable(newX, newY, this, checkBeings: true))
        {
            // Movement blocked - get reason for feedback.
            string reason = Position.Place.GetBlockageReason(newX, newY);
            Console.WriteLine($"{Name} can't move there: {reason}");
            return false;
        }
        
        // Check if we have action points.
        if (ActionPoints <= 0)
        {
            Console.WriteLine($"{Name} is out of action points!");
            return false;
        }
        
        // Get movement cost for destination terrain.
        float cost = Position.Place.GetMovementCost(newX, newY, this);
        int apCost = (int)Math.Ceiling(cost); // Round up
        
        // Check if we can afford the movement.
        if (ActionPoints < apCost)
        {
            Console.WriteLine($"{Name} needs {apCost} AP but only has {ActionPoints} AP!");
            return false;
        }
        
        // For diagonal movement, check if path is clear.
        if (dx != 0 && dy != 0)
        {
            if (!Position.Place.CanMoveTo(Position.X, Position.Y, newX, newY, this))
            {
                Console.WriteLine($"{Name} can't squeeze through diagonally!");
                return false;
            }
        }
        
        // Perform the actual move.
        Position.Place.MoveObject(this, newX, newY);
        
        // Consume action points based on terrain cost.
        DecreaseActionPoints(apCost);
        
        // Provide feedback about movement cost.
        if (apCost > 1)
        {
            var terrain = Position.Place.GetTerrain(newX, newY);
            Console.WriteLine($"{Name} moved to ({newX},{newY}) through {terrain?.Name} (cost: {apCost} AP, {ActionPoints} AP remaining)");
        }
        else
        {
            Console.WriteLine($"{Name} moved to ({newX},{newY}) ({ActionPoints} AP remaining)");
        }
        
        // Check for hazardous terrain.
        if (Position.Place.IsHazardous(newX, newY))
        {
            var terrain = Position.Place.GetTerrain(newX, newY);
            Console.WriteLine($"⚠️  {Name} steps onto hazardous {terrain?.Name}!");
            
            // Apply hazard damage/effects.
            ApplyHazardEffects(terrain);
        }
        
        return true;
    }
    
    /// <summary>
    /// Apply effects from hazardous terrain.
    /// </summary>
    private void ApplyHazardEffects(Terrain? terrain)
    {
        if (terrain == null || !terrain.IsHazardous)
            return;
        
        // Different hazards cause different effects.
        // For now, just apply generic damage.
        int damage = 0;
        
        switch (terrain.Name.ToLower())
        {
            case "lava":
            case "fire":
                damage = Math.Max(1, MaxHitPoints / 10); // 10% of max HP
                Console.WriteLine($"🔥 {Name} is burned for {damage} damage!");
                break;
                
            case "swamp":
            case "poison":
                damage = Math.Max(1, MaxHitPoints / 20); // 5% of max HP
                Console.WriteLine($"☠️  {Name} is poisoned for {damage} damage!");
                // Future: Apply poison status .
                break;
                
            case "ice":
                // Ice doesn't damage, but might cause slipping (future feature).
                Console.WriteLine($"🧊 {Name} slips on ice!");
                break;
                
            default:
                damage = Math.Max(1, MaxHitPoints / 20);
                Console.WriteLine($"💀 {Name} takes {damage} damage from hazardous terrain!");
                break;
        }
        
        if (damage > 0)
        {
            Damage(damage);
            
            if (HitPoints <= 0)
            {
                Console.WriteLine($"💀 {Name} died from {terrain.Name}!");
            }
        }
    }
    
    /// <summary>
    /// Check if this character can move to a location.
    /// Useful for AI and pathfinding.
    /// </summary>
    public bool CanMoveTo(int x, int y)
    {
        if (Position == null)
            return false;
        
        return Position.Place.IsPassable(x, y, this, checkBeings: true);
    }
    
    /// <summary>
    /// Get the AP cost to move to a location.
    /// Useful for AI planning.
    /// </summary>
    public int GetMoveCost(int x, int y)
    {
        if (Position == null)
            return int.MaxValue;
        
        float cost = Position.Place.GetMovementCost(x, y, this);
        return (int)Math.Ceiling(cost);
    }
}
