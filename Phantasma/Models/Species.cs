namespace Phantasma.Models;

public struct Species
{
    public string Tag;
    // struct list
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
    
    // Equipment
    public int NSlots;
    public int Slots;
    public ArmsType Weapon;     // Natural/innate weapon (claws, bite, etc.)
    
    // Magic
    public int NSpells;
    public int Spells;
    
    // Combat
    public int XpVal;           // Reward for killing this type
    public string ArmorDice;    // For scaly or chitinous types
    
    // Skills and Behavior
    public SkillSet Skills;
    public int Stationary;      // Doesn't move?
    
    // Sounds
    // TODO: implement when sound system is ready.
    // public Sound DamageSound;
    // public Sound WalkingSound;
    
    // Callbacks
    // TODO: implement when closure system is ready.
    // public Closure OnDeath;
}