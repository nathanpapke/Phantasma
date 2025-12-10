using System;
using System.Collections.Generic;
using IronScheme;

namespace Phantasma.Models;

/// <summary>
/// Character Class for Player and NPCs
/// </summary>
public class Character : Being
{
    public Party Party { get; set; }
    public Species Species { get; set; }
    public Occupation Occupation { get; set; }
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
    public int ArmorClass { get; set; }
    
    // Vision
    public int VisionRadius { get; set; } = 10;
    
    // Status Effects (duration in turns, 0 = not active)
    public int RevealDuration { get; set; } = 0;       // See invisible/hidden
    public int QuickenDuration { get; set; } = 0;      // Extra actions/turn
    public int TimeStopDuration { get; set; } = 0;     // Freeze other entities
    public int MagicNegatedDuration { get; set; } = 0; // Cannot cast spells
    public int XrayVisionDuration { get; set; } = 0;   // See through walls
    
    // Conversation - IronScheme Closure for NPC dialog
    public object Conversation { get; set; }
    
    // Equipment Slots
    private List<ArmsType> readiedArms;
    private Container inventory;
    
    private int currentArmsIndex = -1;
    
    // Turn Management
    private bool turnEnded;
    
    /// <summary>
    /// Current activity this character is performing.
    /// Tracked independently for solo characters, or synced with party.
    /// </summary>
    private Activity currentActivity = Activity.Idle;
    
    // Party Dynamics
    public int Order { get; set; } = 0;
    public bool IsLeader => Party?.GetLeader() == this;
    public bool CanBeLeader => !IsDead && !IsAsleep && !IsCharmed && Position?.Place != null;
    
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
        ArmorClass = 10;
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
        Occupation = occupation;
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
        player.ArmorClass = 10;
        
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

    /// <summary>
    /// Create a test NPC with conversation for testing.
    /// </summary>
    public static Character CreateTestNPC()
    {
        // Create NPC character.
        var npc = new Character
        {
            //Name = "Guardsman Bob",
            IsPlayer = false,
            Level = 1,
            HP = 50,
            MaxHP = 50//,
            //Conversation = conversationClosure
        };
        npc.SetName("Guardsman Bob");
        
        // Try to load NPC sprite.
        var npcSprite = SpriteManager.GetSprite("npc") ?? SpriteManager.GetSprite("player");
        if (npcSprite == null)
        {
            // Create placeholder sprite.
            npcSprite = new Sprite
            {
                Tag = "npc",
                DisplayChar = '@',
                SourceImage = null
            };
        }
        npc.CurrentSprite = npcSprite;

        return npc;
    }

    /// <summary>
    /// Create a test enemy for combat testing.
    /// </summary>
    public static Character CreateTestEnemy(string name, int hp = 50)
    {
        var enemy = new Character();
        enemy.SetName(name);
        enemy.IsPlayer = false;
        enemy.Level = 1;
        enemy.HP = enemy.MaxHP = hp;
        enemy.MP = enemy.MaxMP = 20;
        enemy.ActionPoints = enemy.MaxActionPoints = 10;
        enemy.ArmorClass = 10;
        enemy.Strength = 10;
        enemy.Dexterity = 10;
        enemy.Intelligence = 8;
    
        // Give enemy a weapon.
        var weapon = ArmsType.TestWeapons.Club;
        enemy.Ready(weapon);
    
        return enemy;
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
    
    /// <summary>
    /// Get current activity (sleeping, working, etc).
    /// </summary>
    public Activity GetActivity()
    {
        // Priority 1: Check if asleep (overrides everything)
        if (IsAsleep)
            return Activity.Sleeping;
    
        // Priority 2: If in a party, use the party's current activity
        // (Party tracks activity based on schedule appointments)
        if (Party != null)
            return Party.CurrentActivity;
    
        // Priority 3: Use character's own tracked activity
        return currentActivity;
    }

    /// <summary>
    /// Set current activity.
    /// Mirrors Nazghul's Character::setActivity().
    /// </summary>
    public void SetActivity(Activity activity)
    {
        currentActivity = activity;
    
        // Sleeping activity controls IsAsleep state.
        if (activity == Activity.Sleeping && !IsAsleep)
        {
            IsAsleep = true;
        }
        else if (activity != Activity.Sleeping && IsAsleep)
        {
            IsAsleep = false;
        }
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
    
        // Check weight.
        int currentBurden = CalculateBurden();
        if (currentBurden + weapon.Weight > Strength)
            return ReadyResult.TooHeavy;
    
        // Just add to list.
        if (!readiedArms.Contains(weapon))
        {
            readiedArms.Add(weapon);
            return ReadyResult.Readied;
        }
    
        return ReadyResult.Readied;
    }

    /// <summary>
    /// Calculate total burden from equipped items.
    /// </summary>
    private int CalculateBurden()
    {
        int burden = 0;
        foreach (var weapon in readiedArms)
        {
            if (weapon != null)
                burden += weapon.Weight;
        }
        return burden;
    }
    
    public bool Unready(ArmsType weapon)
    {
        return readiedArms.Remove(weapon);
    }
    
    public bool HasReadied(ArmsType weapon)
    {
        return readiedArms.Contains(weapon);
    }

    /// <summary>
    /// Attack a target with a weapon.
    /// </summary>
    /// <param name="weapon">Weapon to attack with</param>
    /// <param name="target">Character being attacked</param>
    /// <returns>True if attack succeeded</returns>
    public bool Attack(ArmsType weapon, Character target)
    {
        if (weapon == null || target == null)
        {
            Console.WriteLine("Attack failed: null weapon or target");
            return false;
        }
    
        // Log the attack.
        string attackMsg = $"{GetName()}: {weapon.Name} - {target.GetName()} ";
        Console.Write(attackMsg);
    
        // Check if weapon fires (for missiles/projectiles).
        bool miss = !weapon.Fire(target, Position.X, Position.Y);
    
        // Consume action points and ammo.
        DecreaseActionPoints(weapon.RequiredActionPoints);
        UseAmmo(weapon);
    
        if (miss)
        {
            Console.WriteLine("missed!");
            return false;
        }
    
        // Roll to hit: 1d20 + weapon's to-hit bonus.
        int hit = Dice.Roll("1d20") + Dice.Roll(weapon.ToHitDice);
        int def = target.GetDefend();
    
        Console.WriteLine($"[Hit roll: {hit} vs Defense: {def}]");
    
        if (hit < def)
        {
            Console.WriteLine("barely scratched!");
            return false;
        }
    
        // Roll for damage: weapon damage - target armor.
        int damage = Dice.Roll(weapon.DamageDice);
        int armor = target.GetArmor();
        damage -= armor;
        damage = Math.Max(damage, 0); // Can't have negative damage
    
        Console.WriteLine($"[Damage: {damage} (rolled {Dice.Roll(weapon.DamageDice)} - {armor} armor)]");
    
        // Apply damage.
        target.Damage(damage);
    
        Console.WriteLine($"{target.GetWoundDescription()}!");
    
        // Award XP if target was killed.
        if (target.IsDead)
        {
            int xp = target.GetExperienceValue();
            AddExperience(xp);
            Console.WriteLine($"{GetName()} gains {xp} experience!");
        }
    
        return true;
    }

    /// <summary>
    /// Calculate defense value.
    /// </summary>
    /// <returns>Defense roll total</returns>
    public new int GetDefend()
    {
        int defend = 0;
    
        // TODO: Check if asleep - would return -3.
        // if (IsAsleep()) return -3;
    
        // Sum defense dice from all equipped weapons/armor.
        for (int i = 0; i < readiedArms.Count; i++)
        {
            var arms = readiedArms[i];
            if (arms != null && !string.IsNullOrEmpty(arms.ToDefendDice))
            {
                defend += Dice.Roll(arms.ToDefendDice);
            }
        }
    
        // TODO: Add defense bonus from effects/spells.
        // defend += defenseBonus;
    
        return defend;
    }

    /// <summary>
    /// Calculate armor value.
    /// </summary>
    /// <returns>Armor roll total</returns>
    public new int GetArmor()
    {
        int armor = 0;
    
        // Sum armor dice from all equipped weapons/armor.
        for (int i = 0; i < readiedArms.Count; i++)
        {
            var arms = readiedArms[i];
            if (arms != null && !string.IsNullOrEmpty(arms.ArmorDice))
            {
                armor += Dice.Roll(arms.ArmorDice);
            }
        }
    
        // Add base armor class (from spells like 'protect').
        armor += ArmorClass;
    
        return armor;
    }

    /// <summary>
    /// Iterate through equipped weapons.
    /// </summary>
    public ArmsType? EnumerateArms()
    {
        currentArmsIndex = 0;
        if (readiedArms.Count == 0)
            return null;
        return readiedArms[0];
    }
    
    /// <summary>
    /// Get next equipped weapon in iteration.
    /// </summary>
    public ArmsType? GetNextArms()
    {
        if (currentArmsIndex < 0 || currentArmsIndex >= readiedArms.Count - 1)
            return null;
    
        currentArmsIndex++;
        return readiedArms[currentArmsIndex];
    }

    /// <summary>
    /// Enumerate all weapons for attack purposes.
    /// Used by AI and attack command to try each weapon.
    /// </summary>
    public IEnumerable<ArmsType> EnumerateWeapons()
    {
        foreach (var weapon in readiedArms)
        {
            if (weapon != null)
                yield return weapon;
        }
    }

    /// <summary>
    /// Get attack target for this character.
    /// For player characters, this would be set by targeting UI.
    /// For NPCs, this is set by AI.
    /// </summary>
    private Character? attackTarget;

    public Character? GetAttackTarget()
    {
        return attackTarget;
    }

    public void SetAttackTarget(Character? target)
    {
        attackTarget = target;
    }

    /// <summary>
    /// Check if this character is incapacitated.
    /// </summary>
    public bool IsIncapacitated()
    {
        return IsDead || IsAsleep;
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
        int apCost = (int)Math.Ceiling(cost); // Round up.
        
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
                damage = Math.Max(1, MaxHP / 10); // 10% of max HP
                Console.WriteLine($"🔥 {Name} is burned for {damage} damage!");
                break;
                
            case "swamp":
            case "poison":
                damage = Math.Max(1, MaxHP / 20); // 5% of max HP
                Console.WriteLine($"☠️  {Name} is poisoned for {damage} damage!");
                // Future: Apply poison status .
                break;
                
            case "ice":
                // Ice doesn't damage, but might cause slipping (future feature).
                Console.WriteLine($"🧊 {Name} slips on ice!");
                break;
                
            default:
                damage = Math.Max(1, MaxHP / 20);
                Console.WriteLine($"💀 {Name} takes {damage} damage from hazardous terrain!");
                break;
        }
        
        if (damage > 0)
        {
            Damage(damage);
            
            if (HP <= 0)
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
