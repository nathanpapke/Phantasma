using System;

namespace Phantasma.Models;

public struct Species
{
    public string Tag;
    public string Name;
    
    // Base Attributes
    public int Str;
    public int Intl;
    public int Dex;
    public int Spd;
    public int Vr;
    
    /// <summary>
    /// Movement Mode - determines how this species moves through terrain.
    /// </summary>
    public MovementMode MovementMode;
    
    // HP/MP progression
    public int HpMod;   // Part of base hp contributed by species
    public int HpMult;  // Additional hp per-level contributed by species
    public int MpMod;   // Similar, for mana
    public int MpMult;  // Similar, for mana
    
    // Sprites and Visuals
    public Sprite SleepSprite;
    public bool Visible;        // Is this species visible by default?
    
    // Equipment Slots - Array of slot type masks
    // Each element defines what type of equipment can go in that slot
    // e.g., [SlotMask.Weapon, SlotMask.Weapon, SlotMask.Body, SlotMask.Helm]
    public int NSlots => Slots?.Length ?? 0;
    public int[]? Slots { get; set; }
    
    /// <summary>
    /// Natural/innate weapon (claws, bite, etc.)
    /// Used when no weapon is readied.
    /// </summary>
    public ArmsType Weapon;     // Natural/innate weapon (claws, bite, etc.)
    
    // Magic - Innate spells this species knows
    public int NSpells => Spells?.Length ?? 0;
    public string[]? Spells { get; set; }
    
    // Combat
    public int XpVal;           // Reward for killing this type
    public string ArmorDice;    // For scaly or chitinous types
    
    // Skills and Behavior
    public SkillSet Skills;
    public int Stationary;      // Doesn't move?
    
    // Sounds
    
    /// <summary>
    /// Sound played when this species takes damage.
    /// </summary>
    public Sound? DamageSound { get; set; }
    
    /// <summary>
    /// Sound played when this species moves.
    /// </summary>
    public Sound? MovementSound { get; set; }
    
    /// <summary>
    /// Scheme closure to run when a creature of this species dies.
    /// </summary>
    public object? OnDeath { get; set; }
    
    /// <summary>
    /// Default constructor - creates a basic humanoid species.
    /// </summary>
    public Species()
    {
        // Default humanoid: 2 hands for weapons/shields
        Slots = new int[]
        {
            ArmsType.Slots.Weapon,  // Slot 0: Primary hand
            ArmsType.Slots.Weapon,  // Slot 1: Secondary hand (or shield)
        };
    }
    
    /// <summary>
    /// Create a species with specified slot configuration.
    /// </summary>
    public Species(string tag, string name, int[] slots) : this()
    {
        Tag = tag;
        Name = name;
        if (slots != null && slots.Length > 0)
        {
            Slots = slots;
        }
    }
    
    /// <summary>
    /// Create a standard humanoid species with full equipment slots.
    /// </summary>
    public static Species CreateHumanoid(string tag, string name)
    {
        return new Species
        {
            Tag = tag,
            Name = name,
            Slots = new int[]
            {
                ArmsType.Slots.Weapon,  // Right hand
                ArmsType.Slots.Weapon,  // Left hand (or 2nd weapon slot)
                ArmsType.Slots.Body,    // Torso armor
                ArmsType.Slots.Helm,    // Head
                ArmsType.Slots.Boots,   // Feet
                ArmsType.Slots.Gloves,  // Hands
                ArmsType.Slots.Amulet,  // Neck
                ArmsType.Slots.Ring,    // Finger
            }
        };
    }
    
    /// <summary>
    /// Create a beast species with no equipment slots (uses natural weapons).
    /// </summary>
    public static Species CreateBeast(string tag, string name, ArmsType? naturalWeapon = null)
    {
        return new Species
        {
            Tag = tag,
            Name = name,
            Slots = Array.Empty<int>(),  // No equipment slots
            Weapon = naturalWeapon
        };
    }
}
