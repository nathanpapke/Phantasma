using System;
using System.Collections.Generic;
using System.Linq;
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
    private ArmsType?[]? readiedArms;
    private Container inventory;
    
    private int armsIndex = -1;
    
    /// <summary>
    /// Current burden from equipped items (total weight).
    /// Cannot exceed Strength.
    /// </summary>
    private int burden = 0;
    
    /// <summary>
    /// Current arms during enumeration.
    /// </summary>
    private ArmsType? currentArms;
    
    /// <summary>
    /// Defense bonus from spells/effects.
    /// </summary>
    private int defenseBonus = 0;
    
    /// <summary>
    /// Flag indicating NPC needs to re-equip.
    /// </summary>
    private bool needsRearm = false;
    
    /// <summary>
    /// Spells known by this character.
    /// </summary>
    public HashSet<SpellType> KnownSpells { get; set; } = new();
    
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
        Species = new Species();
        Level = 1;
        Strength = 10;
        Intelligence = 10;
        Dexterity = 10;
        ArmorClass = 10;
        
        // Initialize equipment slots based on species.
        InitializeEquipmentSlots();
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
        MaxHP = GetMaxHp();
        MaxMP = GetMaxMana();
        
        // Initialize equipment slots based on species.
        InitializeEquipmentSlots();
    }
    
    /// <summary>
    /// Initialize equipment slots array based on species definition.
    /// </summary>
    private void InitializeEquipmentSlots()
    {
        int nSlots = Species.NSlots;
        if (nSlots > 0)
        {
            readiedArms = new ArmsType?[nSlots];
            // All slots start empty (null).
        }
        else
        {
            readiedArms = null;
        }
        burden = 0;
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
    
    public int GetMaxHp()
    {
        int baseMod = HpMod + Species.HpMod;
        int mult = HpMult + Species.HpMult;
    
        // Add occupation if present
        if (Occupation.Tag != null)  // Check if occupation is set
        {
            baseMod += Occupation.HpMod;
            mult += Occupation.HpMult;
        }
    
        mult = Math.Max(0, mult);
        return baseMod + (Level * mult);
    }

    public int GetMaxMana()
    {
        int baseMod = MpMod + Species.MpMod;
        int mult = MpMult + Species.MpMult;
    
        if (Occupation.Tag != null)
        {
            baseMod += Occupation.MpMod;
            mult += Occupation.MpMult;
        }
    
        mult = Math.Max(0, mult);
        return baseMod + (Level * mult);
    }
    
    public void AddExperience(int xp)
    {
        Experience += xp;
        CheckLevelUp();
    }
    
    private void CheckLevelUp()
    {
        // Nazghul formula: 2^(level+7)
        double xpNeeded = Math.Pow(2, Level + 7);
    
        while (Experience >= xpNeeded)
        {
            Level++;
            MaxHP = GetMaxHp();
            MaxMP = GetMaxMana();
            HP = MaxHP;
            MP = MaxMP;
            Console.WriteLine($"{GetName()} gains level {Level}!");
            xpNeeded = Math.Pow(2, Level + 7);
        }
    }
    
    // ========================================================================
    // READY/UNREADY - Core equipment management
    // Mirrors Nazghul's Character::ready() and Character::unready()
    // ========================================================================
    
    /// <summary>
    /// Ready (equip) an arms item.
    /// Finds an appropriate empty slot and places the item there.
    /// Handles two-handed weapons requiring two slots.
    /// </summary>
    /// <param name="arms">The weapon/armor to ready</param>
    /// <returns>Result indicating success or failure reason</returns>
    public ReadyResult Ready(ArmsType? arms)
    {
        if (arms == null)
            return ReadyResult.WrongType;
        
        // No slots available for this species?
        if (readiedArms == null || Species.Slots == null || Species.NSlots == 0)
            return ReadyResult.WrongType;
        
        bool foundSlotType = false;
        int slotMask = arms.SlotMask;
        
        // Check if adding this would exceed carrying capacity
        if (burden + arms.Weight > Strength)
            return ReadyResult.TooHeavy;
        
        // Search for an empty slot of the correct type
        for (int i = 0; i < Species.NSlots; i++)
        {
            // Is the slot the right type?
            if ((slotMask & Species.Slots[i]) == 0)
                continue;
            
            foundSlotType = true;
            
            // Is the slot occupied?
            if (readiedArms[i] != null)
                continue;
            
            // At this point we've found an empty slot of the correct type.
            // If this is a two-handed item then we also need the next slot to be empty.
            if (arms.NumHands == 2)
            {
                // Need another slot
                if (i >= Species.NSlots - 1)
                    continue;
                
                // Is the next slot occupied?
                if (readiedArms[i + 1] != null)
                    continue;
                
                // Is the next slot the right type?
                if ((slotMask & Species.Slots[i + 1]) == 0)
                    continue;
                
                // Two-handed: occupy both slots.
                readiedArms[i + 1] = arms;
            }
            
            // Ready the item.
            readiedArms[i] = arms;
            burden += arms.Weight;
            return ReadyResult.Readied;
        }
        
        // We found slots of the right type, but they were all occupied
        if (foundSlotType)
            return ReadyResult.NoAvailableSlot;
        
        // This species doesn't have slots for this type of equipment
        return ReadyResult.WrongType;
    }
    
    /// <summary>
    /// Unready (unequip) an arms item.
    /// Removes the item from its slot(s).
    /// </summary>
    /// <param name="arms">The weapon/armor to unready</param>
    /// <returns>True if item was found and unreadied</returns>
    public bool Unready(ArmsType? arms)
    {
        if (arms == null || readiedArms == null || Species.NSlots == 0)
            return false;
        
        for (int i = 0; i < Species.NSlots; i++)
        {
            // Is it in this slot?
            if (readiedArms[i] != arms)
                continue;
            
            // Is this a 2-handed item (in which case it should be in the next slot too)?
            if (arms.NumHands == 2)
            {
                if (i < Species.NSlots - 1 && readiedArms[i + 1] == arms)
                {
                    readiedArms[i + 1] = null;
                }
            }
            
            // Unready the item.
            readiedArms[i] = null;
            burden -= arms.Weight;
            
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if an arms item is currently readied.
    /// </summary>
    public bool HasReadied(ArmsType? arms)
    {
        if (arms == null || readiedArms == null)
            return false;
        
        for (var weapon = EnumerateArms(); weapon != null; weapon = GetNextArms())
        {
            if (weapon == arms)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// Get the current burden (total weight of equipped items).
    /// </summary>
    public int GetBurden() => burden;
    
    /// <summary>
    /// Get the number of equipment slots this character has.
    /// </summary>
    public int GetNumSlots() => Species.NSlots;
    
    /// <summary>
    /// Get the equipment in a specific slot.
    /// </summary>
    public ArmsType? GetArmsInSlot(int slot)
    {
        if (readiedArms == null || slot < 0 || slot >= Species.NSlots)
            return null;
        return readiedArms[slot];
    }
    
    // ========================================================================
    // ARMS ENUMERATION - Iterate through equipped items
    // Mirrors Nazghul's Character::enumerateArms(), getNextArms(), etc.
    // ========================================================================
    
    /// <summary>
    /// Begin enumerating all equipped arms.
    /// Call GetNextArms() repeatedly to iterate.
    /// </summary>
    /// <returns>First equipped arms, or null if none</returns>
    public ArmsType? EnumerateArms()
    {
        armsIndex = -1;
        currentArms = null;
        return GetNextArms();
    }
    
    /// <summary>
    /// Get the next equipped arms in enumeration.
    /// Skips duplicate entries for two-handed weapons.
    /// </summary>
    /// <returns>Next equipped arms, or null if done</returns>
    public ArmsType? GetNextArms()
    {
        if (readiedArms == null || Species.NSlots == 0)
        {
            currentArms = null;
            return null;
        }
        
        // Advance to the next slot.
        armsIndex++;
        
        // Search remaining slots for an item.
        for (; armsIndex < Species.NSlots; armsIndex++)
        {
            // Is anything in this slot?
            if (readiedArms[armsIndex] == null)
                continue;
            
            // Is this just another slot for the same weapon (two-handed)?
            if (currentArms == readiedArms[armsIndex] && currentArms?.NumHands == 2)
                continue;
            
            currentArms = readiedArms[armsIndex];
            return currentArms;
        }
        
        currentArms = null;
        return null;
    }
    
    /// <summary>
    /// Begin enumerating weapons (arms that can deal damage).
    /// Falls back to species natural weapon if none equipped.
    /// </summary>
    public ArmsType? EnumerateWeapons()
    {
        armsIndex = -1;
        currentArms = null;
        var weapon = GetNextWeapon();
        
        // If no weapon equipped, use species natural weapon.
        if (weapon == null)
            weapon = Species.Weapon;
        
        return weapon;
    }
    
    /// <summary>
    /// Get the next weapon in enumeration.
    /// Only returns arms with damage dice > 0.
    /// </summary>
    public ArmsType? GetNextWeapon()
    {
        do
        {
            GetNextArms();
        }
        while (currentArms != null && Dice.Average(currentArms.DamageDice) <= 0);
        
        return currentArms;
    }
    
    /// <summary>
    /// Get all weapons for IEnumerable iteration.
    /// </summary>
    public IEnumerable<ArmsType> GetAllWeapons()
    {
        for (var weapon = EnumerateWeapons(); weapon != null; weapon = GetNextWeapon())
        {
            yield return weapon;
        }
    }
    
    /// <summary>
    /// Get all equipped arms for IEnumerable iteration.
    /// </summary>
    public IEnumerable<ArmsType> GetAllArms()
    {
        for (var arms = EnumerateArms(); arms != null; arms = GetNextArms())
        {
            yield return arms;
        }
    }
    
    /// <summary>
    /// Get the current weapon (for combat).
    /// </summary>
    public ArmsType? GetCurrentWeapon()
    {
        return currentArms ?? Species.Weapon;
    }
    
    // ========================================================================
    // INVENTORY MANAGEMENT
    // ========================================================================
    
    /// <summary>
    /// Get this character's inventory container.
    /// For player-controlled characters, returns party inventory.
    /// For NPCs, returns personal inventory.
    /// </summary>
    public Container? GetInventoryContainer()
    {
        if (IsPlayer && Party?.Inventory != null)
            return Party.Inventory;
        return inventory;
    }
    
    /// <summary>
    /// Set this character's personal inventory container.
    /// </summary>
    public void SetInventoryContainer(Container? container)
    {
        inventory = container;
    }
    
    /// <summary>
    /// Check if character has ammo for a weapon.
    /// Returns count of available ammo (0 = none, 1+ = has ammo).
    /// </summary>
    public bool HasAmmo(ArmsType weapon)
    {
        if (weapon.HasUbiquitousAmmo)
            return true;  // Unlimited ammo
        
        var container = GetInventoryContainer();
        if (container == null)
            return false;
        
        if (weapon.IsMissileWeapon())
        {
            var ammoType = weapon.GetMissileType();
            if (ammoType == null)
                return false;
            
            var entry = container.Search(ammoType);
            return entry?.Quantity > 0;
        }
        else if (weapon.IsThrown)
        {
            var entry = container.Search(weapon);
            if (entry == null)
            {
                // No more in inventory, unready it.
                Unready(weapon);
                return false;
            }
            return entry.Quantity > 0;
        }
        
        // Melee weapons don't need ammo.
        return true;
    }
    
    /// <summary>
    /// Consume ammo for a weapon after firing.
    /// </summary>
    public int UseAmmo(ArmsType weapon)
    {
        if (weapon.HasUbiquitousAmmo)
            return 0;
        
        var container = GetInventoryContainer();
        if (container == null)
            return 1;
        
        if (weapon.IsMissileWeapon())
        {
            var ammoType = weapon.GetMissileType();
            if (ammoType != null)
                return 0;
            
            var entry = container.Search(ammoType);
            return entry?.Quantity ?? 0;
        }
        else if (weapon.IsThrown)
        {
            var entry = container.Search(weapon);
            if (entry == null)
            {
                // No more in inventory, unready it
                Unready(weapon);
                return 0;
            }
            return entry.Quantity;
        }
        
        // Melee weapons don't need ammo.
        return 1;
    }
    
    /// <summary>
    /// Check if character has an item type in inventory.
    /// </summary>
    public bool HasInInventory(ObjectType type)
    {
        var container = GetInventoryContainer();
        return container?.Search(type) != null;
    }
    
    // ========================================================================
    // NPC AUTO-EQUIP
    // ========================================================================
    
    /// <summary>
    /// Automatically equip best available items from inventory.
    /// Used by NPCs to arm themselves.
    /// Simplified version of Nazghul's knapsack algorithm.
    /// </summary>
    public void ArmThyself()
    {
        if (IsPlayer || inventory == null)
            return;
        
        // Simple greedy approach: try to ready each arms item.
        var armsItems = new List<ArmsType>();
        
        foreach (var entry in inventory.GetContents())
        {
            if (entry.Type is ArmsType arms)
            {
                armsItems.Add(arms);
            }
        }
        
        // Sort by value (damage + armor) descending.
        armsItems.Sort((a, b) =>
        {
            int aValue = Dice.Average(a.DamageDice) * a.Range + Dice.Average(a.ArmorDice);
            int bValue = Dice.Average(b.DamageDice) * b.Range + Dice.Average(b.ArmorDice);
            return bValue.CompareTo(aValue);
        });
        
        // Try to ready each item.
        foreach (var arms in armsItems)
        {
            if (Ready(arms) == ReadyResult.Readied)
            {
                inventory.RemoveItem(arms, 1);
            }
        }
        
        needsRearm = false;
    }
    
    /// <summary>
    /// Check if NPC needs to re-equip (ran out of ammo, etc).
    /// </summary>
    public bool NeedsToRearm()
    {
        // Player party members don't auto-rearm.
        if (Party != null && Party == Phantasma.MainSession.Party)
            return false;
        
        return needsRearm;
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
        for (int i = 0; i < readiedArms.Length; i++)
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
        for (int i = 0; i < readiedArms.Length; i++)
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
    /*
    /// <summary>
    /// Iterate through equipped weapons.
    /// </summary>
    public ArmsType? EnumerateArms()
    {
        armsIndex = 0;
        if (readiedArms.Count == 0)
            return null;
        return readiedArms[0];
    }
    
    /// <summary>
    /// Get next equipped weapon in iteration.
    /// </summary>
    public ArmsType? GetNextArms()
    {
        if (armsIndex < 0 || armsIndex >= readiedArms.Count - 1)
            return null;
    
        armsIndex++;
        return readiedArms[armsIndex];
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
    */
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
    /*
    public bool HasAmmo(ArmsType weapon)
    {
        // Simplified - always have ammo for now
        return true;
    }
    
    public void UseAmmo(ArmsType weapon)
    {
        // Will implement when we add ammo tracking
    }
    */
    public void TakeOut(ArmsType weapon, int count)
    {
        // Will implement with inventory
    }
    
    public void Add(ArmsType weapon, int count)
    {
        // Will implement with inventory
    }
    /*
    public Container GetInventoryContainer()
    {
        return inventory;
    }
    */
    /// <summary>
    /// Learn a new spell.
    /// </summary>
    public void LearnSpell(SpellType spell)
    {
        if (!KnownSpells.Contains(spell))
        {
            KnownSpells.Add(spell);
            Console.WriteLine($"{Name} learned {spell.Name}!");
        }
    }

    /// <summary>
    /// Forget a spell.
    /// </summary>
    public void ForgetSpell(SpellType spell)
    {
        if (KnownSpells.Remove(spell))
        {
            Console.WriteLine($"{Name} forgot {spell.Name}!");
        }
    }

    /// <summary>
    /// Check if character knows a spell.
    /// </summary>
    public bool KnowsSpell(SpellType spell)
    {
        return KnownSpells.Contains(spell);
    }

    /// <summary>
    /// Get all spells the character can currently cast.
    /// (Has enough MP and reagents)
    /// </summary>
    public IEnumerable<SpellType> GetCastableSpells()
    {
        return KnownSpells.Where(spell => 
            spell.CanAfford(this) && spell.HasReagents(this));
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
        
        if (Position.Place == null)
        {
            Console.WriteLine($"{Name} has a position but no place!");
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
    
    public void Kill()
    {
        // Only NPCs (characters not in player party) drop their items.
        bool isInPlayerParty = IsPlayer || (Party != null && Party.IsPlayerParty);
        
        // NPCs drop their items.
        if (!isInPlayerParty  && IsOnMap())
        {
            DropReadyArms();
            DropItems();
        }
        
        if (isInPlayerParty )
        {
            Console.WriteLine($"{GetName()} has fallen!!");
        }
        
        HP = 0;
        
        // Run species on-death procedure.
        if (Species.OnDeath != null)
        {
            // Execute closure: closure_exec(species->on_death, "p", this).
        }
        
        // Remove from map (careful - can delete this object).
        Remove();
    }
    
    public void Resurrect()
    {
        // Only player party members can be resurrected.
        bool isInPlayerParty = IsPlayer || (Party != null && Party.IsPlayerParty);
        if (!isInPlayerParty)
            return;
    
        // Restore some HP - this also clears IsDead since IsDead is computed as HP <= 0.
        HP = Math.Min(10, MaxHP);
    
        // TODO: StatusFlash(Order, StatusColor.Blue);
    
        // If already on map, we're done.
        if (IsOnMap())
            return;
    
        // Place near party leader.
        var leader = Party?.GetLeader();
        if (leader?.Position?.Place == null)
            return;
    
        // TODO: Nazghul's putOnMap searches for nearby open spot with radius.
        // For now, just place at leader's position.
        leader.Position.Place.AddObject(this, leader.Position.X, leader.Position.Y);
    }
    
    public void SetHp(int val)
    {
        HP = Math.Clamp(val, 0, MaxHP);
        if (HP == 0)
            Kill();
    }
    
    public void DropReadyArms()
    {
        if (readiedArms == null)
            return;
    
        var place = Position?.Place;
        if (place == null)
            return;
    
        int x = Position.X;
        int y = Position.Y;
    
        for (int i = 0; i < Species.NSlots; i++)
        {
            // Anything in this slot?
            if (readiedArms[i] == null)
                continue;
        
            // Skip duplicate entries for two-handed weapons.
            if (i > 0 && readiedArms[i] == readiedArms[i - 1])
                continue;
        
            // Create an Item and drop it on the map.
            var item = new Item
            {
                Type = readiedArms[i],
                Name = readiedArms[i].Name,
                Quantity = 1
            };
        
            place.AddObject(item, x, y);
        
            // Unready it.
            Unready(readiedArms[i]);
        }
    }
    
    public bool DropItems()
    {
        if (inventory == null)
            return false;
    
        // Don't drop empty containers.
        if (inventory.IsEmpty())
            return false;
    
        // Place the container on the map at character's location.
        var place = Position?.Place;
        if (place == null)
            return false;
    
        place.AddObject(inventory, Position.X, Position.Y);
    
        // Clear our reference.
        inventory = null;
    
        return true;
    }
    
    // ========================================================================
    // STATIC FACTORY METHODS
    // ========================================================================
    
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
}
