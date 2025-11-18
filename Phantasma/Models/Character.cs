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
    public int HpMod { get; set; }
    public int HpMult { get; set; }
    public int MpMod { get; set; }
    public int MpMult { get; set; }
    public int Strength { get; set; }
    public int Intelligence { get; set; }
    public int Dexterity { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
    
    // Vision
    public int VisionRadius { get; set; } = 10;
    
    // Equipment slots
    private List<ArmsType> readiedArms;
    private Container inventory;
    
    // Turn management
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
    }

    public Character(string tag, string name,
        Sprite sprite,
        Species species, Occupation occ,
        int str, int intl,
        int dex, int hpmod, int hpmult,
        int mpmod, int mpmult,
        int hp, int xp, int mp,
        int lvl) : this()
    {
        SetName(name);
        CurrentSprite = sprite;
        Species = species;
        Occ = occ;
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
        
        // Calculate max values
        MaxHP = HpMod + (HpMult * Level);
        MaxMP = MpMod + (MpMult * Level);
    }
    
    /// <summary>
    /// Create a simple test player character
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
        
        // Try to load player sprite.
        var playerSprite = SpriteManager.GetSprite("player");
        if (playerSprite != null)
        {
            player.CurrentSprite = playerSprite;
        }
        else
        {
            // Create a placeholder sprite
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
}
