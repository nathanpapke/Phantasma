using System;

namespace Phantasma.Models;

/// <summary>
/// Weapon and Armor type definition - port of Nazghul's ArmsType.
/// Represents weapons, shields, armor, and other combat equipment.
/// </summary>
public class ArmsType : ObjectType
{
    // Core Properties
    public int SlotMask { get; protected set; }
    public int NumHands { get; protected set; }
    public int Range { get; protected set; }
    public int RequiredActionPoints { get; protected set; }
    public int Weight { get; set; }
    
    // Dice Strings
    public string ToHitDice { get; protected set; } = "0";
    public string ToDefendDice { get; protected set; } = "0";
    public string DamageDice { get; protected set; } = "0";
    public string ArmorDice { get; protected set; } = "0";
    
    // Weapon Type Flags
    public bool IsThrown { get; protected set; }
    public bool HasUbiquitousAmmo { get; protected set; }
    
    // Missile/Projectile (for ranged weapons)
    protected Missile? missile;
    protected ArmsType? missileType;
    
    // Sound Effect for Firing (stub for now)
    // protected Sound? fireSound;
    
    /// <summary>
    /// Equipment Slot Masks (bitfield)
    /// Example: SLOT_WEAPON = 0x01, SLOT_SHIELD = 0x02, SLOT_BODY = 0x04
    /// </summary>
    public static class Slots
    {
        public const int None = 0x00;
        public const int Weapon = 0x01;
        public const int Shield = 0x02;
        public const int Body = 0x04;
        public const int Helm = 0x08;
        public const int Boots = 0x10;
        public const int Gloves = 0x20;
        public const int Amulet = 0x40;
        public const int Ring = 0x80;
    }
    
    /// <summary>
    /// Default Constructor for Deserialization
    /// </summary>
    public ArmsType() : base()
    {
        Layer = ObjectLayer.Item;
        SlotMask = Slots.Weapon;
        NumHands = 1;
        Range = 1;
        RequiredActionPoints = 1;
        ToHitDice = "0";
        ToDefendDice = "0";
        DamageDice = "1d2";
        ArmorDice = "0";
        IsThrown = false;
        HasUbiquitousAmmo = false;
    }
    
    /// <summary>
    /// Full Constructor for Arms Type
    /// </summary>
    /// <param name="tag">Unique identifier</param>
    /// <param name="name">Display name</param>
    /// <param name="sprite">Visual representation</param>
    /// <param name="slotMask">Which slots this can be equipped in</param>
    /// <param name="toHitDice">Attack bonus dice (e.g., "1d4")</param>
    /// <param name="toDefendDice">Defense bonus when equipped (e.g., "1d2")</param>
    /// <param name="numHands">1 or 2 handed</param>
    /// <param name="range">Attack range in tiles</param>
    /// <param name="weight">Weight in arbitrary units</param>
    /// <param name="damageDice">Damage dice (e.g., "2d6+3")</param>
    /// <param name="armorDice">Armor protection dice (e.g., "1d4")</param>
    /// <param name="requiredActionPoints">AP cost to attack</param>
    /// <param name="thrown">Is this a thrown weapon?</param>
    /// <param name="ubiquitousAmmo">Infinite ammo (like arrows for practice bow)</param>
    /// <param name="missileType">For ranged weapons, the projectile type</param>
    public ArmsType(
        string tag,
        string name,
        Sprite? sprite,
        int slotMask,
        string toHitDice,
        string toDefendDice,
        int numHands,
        int range,
        int weight,
        string damageDice,
        string armorDice,
        int requiredActionPoints,
        bool thrown = false,
        bool ubiquitousAmmo = false,
        ArmsType? missileType = null)
        : base(tag, name, ObjectLayer.Item)
    {
        this.Sprite = sprite;
        this.SlotMask = slotMask;
        this.ToHitDice = toHitDice ?? "0";
        this.ToDefendDice = toDefendDice ?? "0";
        this.NumHands = numHands;
        this.Range = range;
        this.Weight = weight;
        this.DamageDice = damageDice ?? "1d2";
        this.ArmorDice = armorDice ?? "0";
        this.RequiredActionPoints = requiredActionPoints;
        this.IsThrown = thrown;
        this.HasUbiquitousAmmo = ubiquitousAmmo;
        
        // Set up missile/projectile.
        if (missileType != null)
        {
            this.missileType = missileType;
            this.missile = new Missile(missileType);
        }
        
        // Thrown weapons are their own missiles.
        if (thrown)
        {
            SetMissileType(this);
        }
    }
    
    /// <summary>
    /// Check if this is a missile weapon (bow, crossbow, etc).
    /// Missile weapons have a separate ammo type.
    /// </summary>
    public bool IsMissileWeapon()
    {
        return missile != null && !IsThrown;
    }
    
    /// <summary>
    /// Check if this is a thrown weapon (dagger, spear, etc).
    /// </summary>
    public bool IsThrownWeapon()
    {
        return IsThrown;
    }
    
    /// <summary>
    /// Get the projectile type for this weapon.
    /// For bows: arrows. For crossbows: bolts. For thrown: self.
    /// </summary>
    public ArmsType? GetMissileType()
    {
        if (missile == null)
            return null;
        return missileType;
    }
    
    /// <summary>
    /// Set the projectile type.
    /// </summary>
    public void SetMissileType(ArmsType? newMissileType)
    {
        missile = null;
        missileType = null;
        
        if (newMissileType == null)
            return;
        
        missile = new Missile(newMissileType);
        missileType = newMissileType;
    }
    
    /// <summary>
    /// Get the ammunition type required.
    /// For thrown weapons: returns self.
    /// For missile weapons: returns the missile type.
    /// For melee weapons: returns null.
    /// </summary>
    public ArmsType? GetAmmoType()
    {
        if (IsThrown)
            return this;
        if (missile == null)
            return null;
        return missileType;
    }
    
    /// <summary>
    /// Set whether this is a thrown weapon.
    /// </summary>
    public void SetThrown(bool val)
    {
        if (val == IsThrown)
            return;
        
        IsThrown = val;
        
        if (!val)
        {
            // No longer thrown, clear missile if it was self.
            if (missileType == this)
            {
                missile = null;
                missileType = null;
            }
            return;
        }
        
        // Now thrown - use self as missile.
        SetMissileType(this);
    }
    
    /// <summary>
    /// Fire this weapon at a target.
    /// Just checks range and returns hit/miss.
    /// </summary>
    /// <param name="target">Being being attacked</param>
    /// <param name="originX">Attacker's X position</param>
    /// <param name="originY">Attacker's Y position</param>
    /// <returns>True if projectile hits, false if misses</returns>
    public virtual bool Fire(Being target, int originX, int originY)
    {
        if (!IsMissileWeapon() && !IsThrownWeapon())
            return true; // Melee always "hits" (actual hit is determined by attack roll).
        
        // Check range.
        int distance = CalculateDistance(
            originX, originY,
            target.Position.X, target.Position.Y);
        
        if (distance > Range)
            return false; // Out of range
        
        // Set up the missile for animation.
        if (missile != null)
        {
            missile.Position = new Location(target.GetPlace(), originX, originY);
            
            // Check if we hit.
            if (!missile.HitTarget())
                return false; // Missed or blocked
        }
        
        return true; // Hit
    }
    
    /// <summary>
    /// Fire weapon at a specific location (not targeting a being).
    /// Used for targeting terrain, mechanisms, or empty tiles.
    /// </summary>
    /// <param name="place">The map/place</param>
    /// <param name="originX">Attacker's X position</param>
    /// <param name="originY">Attacker's Y position</param>
    /// <param name="targetX">Target X coordinate</param>
    /// <param name="targetY">Target Y coordinate</param>
    /// <returns>True if animation completed</returns>
    public virtual bool Fire(Place place, int originX, int originY, int targetX, int targetY)
    {
        if (!IsMissileWeapon() && !IsThrownWeapon())
            return true; // Melee doesn't fire at locations.
        
        // Check range.
        int distance = CalculateDistance(originX, originY, targetX, targetY);
        if (distance > Range)
            return false;
        
        // Set up the missile for animation.
        if (missile != null)
        {
            missile.Position = new Location(place, originX, originY);
        }
        
        return true;
    }
    
    /// <summary>
    /// Fire weapon in a direction (for "spray" attacks like cannonballs).
    /// Fires at maximum range in the given direction.
    /// </summary>
    /// <param name="place">The map/place</param>
    /// <param name="originX">Attacker's X position</param>
    /// <param name="originY">Attacker's Y position</param>
    /// <param name="dx">Direction X (-1, 0, 1)</param>
    /// <param name="dy">Direction Y (-1, 0, 1)</param>
    /// <param name="user">Object firing (for sound effects)</param>
    /// <returns>True if hit something, false if missed</returns>
    public virtual bool FireInDirection(Place place, int originX, int originY, int dx, int dy, Object user)
    {
        if (!IsMissileWeapon() && !IsThrownWeapon())
            return false;
        
        // TODO: Play fire sound when sound system is implemented (Task 25)
        // if (fireSound != null)
        //     Sound.Play(fireSound, Sound.MaxVolume);
        
        // Calculate target at maximum range in this direction.
        int targetX = dx * Range + originX;
        int targetY = dy * Range + originY;
        
        // Set up the missile for animation.
        if (missile != null)
        {
            missile.Position = new Location(place, originX, originY);
            
            // Check if we hit something.
            if (!missile.HitTarget() || missile.GetStruck() == null)
                return false;
            
            // Log the hit.
            var struck = missile.GetStruck();
            Console.WriteLine($"{Name} hit {struck?.Name ?? "something"}!");
            
            // Deal damage to what we hit.
            // (This will be called by the combat system in ctrl_do_attack.)
            
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Calculate Manhattan distance between two points.
    /// </summary>
    private int CalculateDistance(int x1, int y1, int x2, int y2)
    {
        return Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1)); // Chebyshev distance
    }
    
    /// <summary>
    /// Create a simple melee weapon.
    /// </summary>
    public static ArmsType CreateMeleeWeapon(
        string tag,
        string name,
        string damageDice,
        int range = 1,
        int numHands = 1,
        string toHitDice = "0",
        string armorDice = "0")
    {
        return new ArmsType(
            tag: tag,
            name: name,
            sprite: null,
            slotMask: Slots.Weapon,
            toHitDice: toHitDice,
            toDefendDice: "0",
            numHands: numHands,
            range: range,
            weight: 10,
            damageDice: damageDice,
            armorDice: armorDice,
            requiredActionPoints: 1
        );
    }
    
    /// <summary>
    /// Create a ranged weapon (bow, crossbow, etc).
    /// </summary>
    public static ArmsType CreateRangedWeapon(
        string tag,
        string name,
        string damageDice,
        int range,
        ArmsType ammoType,
        int numHands = 2,
        string toHitDice = "0",
        int weight = 15)
    {
        return new ArmsType(
            tag: tag,
            name: name,
            sprite: null,
            slotMask: Slots.Weapon,
            toHitDice: toHitDice,
            toDefendDice: "0",
            numHands: numHands,
            range: range,
            weight: weight,
            damageDice: damageDice,
            armorDice: "0",
            requiredActionPoints: 1,
            thrown: false,
            ubiquitousAmmo: false,
            missileType: ammoType
        );
    }
    
    /// <summary>
    /// Create a thrown weapon.
    /// </summary>
    public static ArmsType CreateThrownWeapon(
        string tag,
        string name,
        string damageDice,
        int range,
        string toHitDice = "0",
        int weight = 5)
    {
        return new ArmsType(
            tag: tag,
            name: name,
            sprite: null,
            slotMask: Slots.Weapon,
            toHitDice: toHitDice,
            toDefendDice: "0",
            numHands: 1,
            range: range,
            weight: weight,
            damageDice: damageDice,
            armorDice: "0",
            requiredActionPoints: 1,
            thrown: true,
            ubiquitousAmmo: false
        );
    }
    
    /// <summary>
    /// Create a simple armor piece.
    /// </summary>
    public static ArmsType CreateArmor(
        string tag,
        string name,
        string armorDice,
        int slotMask,
        string toDefendDice = "0")
    {
        return new ArmsType(
            tag: tag,
            name: name,
            sprite: null,
            slotMask: slotMask,
            toHitDice: "0",
            toDefendDice: toDefendDice,
            numHands: 0,
            range: 0,
            weight: 20,
            damageDice: "0",
            armorDice: armorDice,
            requiredActionPoints: 0
        );
    }
    
    /// <summary>
    /// Create common test weapons for development.
    /// </summary>
    public static class TestWeapons
    {
        public static ArmsType Fists => CreateMeleeWeapon(
            "fists", "Fists", "1d2", 1, 1, "0", "0");
        
        public static ArmsType Dagger => CreateMeleeWeapon(
            "dagger", "Dagger", "1d4", 1, 1, "+1", "0");
        
        public static ArmsType Sword => CreateMeleeWeapon(
            "sword", "Short Sword", "1d6+1", 1, 1, "+2", "0");
        
        public static ArmsType LongSword => CreateMeleeWeapon(
            "longsword", "Long Sword", "1d8+2", 1, 1, "+3", "0");
        
        public static ArmsType GreatSword => CreateMeleeWeapon(
            "greatsword", "Great Sword", "2d6+3", 1, 2, "+1", "0");
        
        public static ArmsType Club => CreateMeleeWeapon(
            "club", "Club", "1d6", 1, 1, "0", "0");
        
        public static ArmsType Spear => CreateMeleeWeapon(
            "spear", "Spear", "1d6+1", 2, 1, "+1", "0");
        
        public static ArmsType Axe => CreateMeleeWeapon(
            "axe", "Battle Axe", "1d8+2", 1, 1, "+1", "0");
        
        public static ArmsType LeatherArmor => CreateArmor(
            "leather", "Leather Armor", "1d2", Slots.Body, "0");
        
        public static ArmsType ChainMail => CreateArmor(
            "chainmail", "Chain Mail", "1d4", Slots.Body, "+1");
        
        public static ArmsType Shield => CreateArmor(
            "shield", "Shield", "1d3", Slots.Shield, "+2");
    }
}

/// <summary>
/// Flags for missile behavior.
/// </summary>
[Flags]
public enum MissileFlags
{
    None = 0,
    IgnoreLOS = 1,
    HitParty = 2
}
