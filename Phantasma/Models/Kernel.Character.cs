using System;
using System.Collections.Generic;
using System.Linq;
using IronScheme;
using IronScheme.Runtime;
using IronScheme.Scripting;

namespace Phantasma.Models;

public partial class Kernel
{
    /// <summary>
    /// (kern-char-get-hp character)
    /// Gets current HP.
    /// </summary>
    public static object CharacterGetHp(object character)
    {
        var c = character as Character;
        if (c == null)
        {
            LoadError("kern-char-get-hp: invalid character");
            return 0;
        }
        return c.HP;
    }
    
    /// <summary>
    /// (kern-char-get-max-hp character)
    /// Gets maximum HP.
    /// </summary>
    public static object CharacterGetMaxHp(object character)
    {
        var c = character as Character;
        if (c == null)
        {
            LoadError("kern-char-get-max-hp: invalid character");
            return 0;
        }
        return c.MaxHP;
    }
    
    /// <summary>
    /// (kern-char-get-level character)
    /// Gets character level.
    /// </summary>
    public static object CharacterGetLevel(object character)
    {
        var c = character as Character;
        if (c == null)
        {
            LoadError("kern-char-get-level: invalid character");
            return 0;
        }
        return c.Level;
    }
    
    /// <summary>
    /// (kern-char-set-hp character value)
    /// Sets the character's HP to the specified value.
    /// Clamps to [0, MaxHP] and triggers kill() if HP reaches 0.
    /// Matches Nazghul's Character::setHp() behavior.
    /// </summary>
    public static object CharacterSetHp(object character, object value)
    {
        if (character is not Character ch)
        {
            Console.WriteLine("[ERROR] kern-char-set-hp: not a character");
            return "#f".Eval();
        }
        
        int hp = Convert.ToInt32(value);
        
        // Clamp to valid range [0, MaxHP].
        hp = Math.Clamp(hp, 0, ch.MaxHP);
        
        // Set the HP.
        ch.HP = hp;
        
        // If HP reaches 0, kill the character.
        if (ch.HP == 0)
        {
            ch.Kill();
        }
        
        return "#f".Eval();
    }
    
    /// <summary>
    /// (kern-char-get-weapons character)
    /// Returns a Scheme list of the character's readied weapons.
    /// </summary>
    public static object CharacterGetWeapons(object character)
    {
        if (character is not Character ch)
        {
            Console.WriteLine("[ERROR] kern-char-get-weapons: not a character");
            return "nil".Eval();
        }
        
        // Build a list of weapons.
        var weapons = new List<object>();
        
        for (var weapon = ch.EnumerateWeapons(); weapon != null; weapon = ch.GetNextWeapon())
        {
            weapons.Add(weapon);
        }
        
        // Convert to Scheme list.
        return ListToScheme(weapons);
    }
    
    /// <summary>
    /// (kern-char-arm-self character)
    /// Makes an NPC automatically equip best items from inventory.
    /// </summary>
    public static object CharacterArmSelf(object[] args)
    {
        if (args == null || args.Length == 0)
        {
            Console.WriteLine("[ERROR] kern-char-arm-self: no arguments");
            return "nil".Eval();
        }
        
        var character = args[0];
        
        if (character is not Character ch)
        {
            Console.WriteLine("[ERROR] kern-char-arm-self: not a character");
            return "nil".Eval();
        }
        
        ch.ArmThyself();
        
        return character;
    }
    
    /// <summary>
    /// (kern-char-get-inventory character)
    /// Returns a Scheme list of (type . count) pairs for inventory contents.
    /// </summary>
    public static object CharacterGetInventory(object character)
    {
        if (character is not Character ch)
        {
            Console.WriteLine("[ERROR] kern-char-get-inventory: not a character");
            return "nil".Eval();
        }
        
        var container = ch.GetInventoryContainer();
        if (container == null)
            return "nil".Eval();
        
        var items = new List<object>();
        
        foreach (var entry in container.GetContents())
        {
            // Create a (type . count) pair.
            var pair = new Cons(entry.Type, entry.Quantity);
            items.Add(pair);
        }
        
        return ListToScheme(items);
    }
    
    /// <summary>
    /// (kern-char-has-ammo? character weapon)
    /// Returns #t if character has ammo for the weapon, #f otherwise.
    /// </summary>
    public static object CharacterHasAmmo(object character, object weapon)
    {
        if (character is not Character ch)
        {
            Console.WriteLine("[ERROR] kern-char-has-ammo?: not a character");
            return "#f".Eval();
        }
        
        if (weapon is not ArmsType arms)
        {
            Console.WriteLine("[ERROR] kern-char-has-ammo?: not an arms type");
            return "#f".Eval();
        }
        
        return ch.HasAmmo(arms);
    }
    
    /// <summary>
    /// (kern-char-ready character arms-type)
    /// Ready an arms type for a character.
    /// Returns #t on success, #f on failure.
    /// </summary>
    public static object CharacterReady(object character, object armsType)
    {
        if (character is not Character ch)
        {
            Console.WriteLine("[ERROR] kern-char-ready: not a character");
            return "#f".Eval();
        }
        
        if (armsType is not ArmsType arms)
        {
            Console.WriteLine("[ERROR] kern-char-ready: not an arms type");
            return "#f".Eval();
        }
        
        var result = ch.Ready(arms);
        return result == Character.ReadyResult.Readied ? "#t".Eval() : "#f".Eval();
    }
    
    /// <summary>
    /// (kern-char-unready character arms-type)
    /// Unready an arms type from a character.
    /// Returns #t on success, #f on failure.
    /// </summary>
    public static object CharacterUnready(object character, object armsType)
    {
        if (character is not Character ch)
        {
            Console.WriteLine("[ERROR] kern-char-unready: not a character");
            return "#f".Eval();
        }
        
        if (armsType is not ArmsType arms)
        {
            Console.WriteLine("[ERROR] kern-char-unready: not an arms type");
            return "#f".Eval();
        }
        
        return ch.Unready(arms) ? "#t".Eval() : "#f".Eval();
    }
    
    /// <summary>
    /// (kern-char-kill character)
    /// Kills the character.
    /// </summary>
    public static object CharacterKill(object character)
    {
        if (character is not Character ch)
        {
            Console.WriteLine("[ERROR] kern-char-kill: not a character");
            return "nil".Eval();
        }
        
        ch.Kill();
        return "nil".Eval();
    }

    /// <summary>
    /// (kern-char-resurrect character)
    /// Resurrects a dead character.
    /// </summary>
    public static object CharacterResurrect(object character)
    {
        if (character is not Character ch)
        {
            Console.WriteLine("[ERROR] kern-char-resurrect: not a character");
            return null;
        }
        
        ch.Resurrect();
        return "nil".Eval();
    }

    /// <summary>
    /// (kern-char-set-ai char closure)
    /// </summary>
    public static object CharacterSetAI(object charArg, object closureArg)
    {
        if (charArg is not Character character) return false;
        character.AIBehavior = (closureArg == null || (closureArg is bool b && !b)) ? null : closureArg;
        return "#t".Eval();
    }
    
    /// <summary>
    /// (kern-char-get-mana char)
    /// </summary>
    public static object CharacterGetMana(object charArg)
    {
        if (charArg is not Character character) return 0;
        return character.MP;
    }
    
    /// <summary>
    /// (kern-char-dec-mana char amount)
    /// </summary>
    public static object CharacterDecreaseMana(object charArg, object amountArg)
    {
        if (charArg is not Character character) return false;
        character.MP = Math.Max(0, character.MP - Convert.ToInt32(amountArg));
        return character;
    }
    
    /// <summary>
    /// (kern-char-attack attacker weapon defender)
    /// </summary>
    public static object CharacterAttack(object[] args)
    {
        object attackerArg, weaponArg, defenderArg;
        
        if (args.Length >= 3)
        {
            attackerArg = args[0];
            weaponArg = args[1];
            defenderArg = args[2];
        }
        else if (args.Length == 1 && args[0] is Cons list)
        {
            // Called via apply with a single list argument.
            var items = list.ToList();
            if (items.Count < 3)
            {
                Console.WriteLine($"[kern-char-attack] Expected 3 args in list, got {items.Count}");
                return "#f".Eval();
            }
            attackerArg = items[0];
            weaponArg = items[1];
            defenderArg = items[2];
        }
        else
        {
            Console.WriteLine($"[kern-char-attack] Expected 3 args, got {args.Length}");
            return "#f".Eval();
        }
        
        // Handle array wrappers from IronScheme.
        if (attackerArg is object[] arr1 && arr1.Length > 0) attackerArg = arr1[0];
        if (weaponArg is object[] arr2 && arr2.Length > 0) weaponArg = arr2[0];
        if (defenderArg is object[] arr3 && arr3.Length > 0) defenderArg = arr3[0];
        
        if (attackerArg is not Character attacker)
        {
            Console.WriteLine($"[kern-char-attack] Invalid attacker: {attackerArg?.GetType().Name}");
            return "#f".Eval();
        }
        if (defenderArg is not Character defender)
        {
            Console.WriteLine($"[kern-char-attack] Invalid defender: {defenderArg?.GetType().Name}");
            return "#f".Eval();
        }
        ArmsType? weapon = weaponArg as ArmsType;
        if (weapon == null)
        {
            Console.WriteLine($"[kern-char-attack] Invalid weapon: {weaponArg?.GetType().Name}");
            return "#f".Eval();
        }
        
        Console.WriteLine($"[kern-char-attack] {attacker.GetName()} attacking {defender.GetName()} with {weapon.Name}");
        return attacker.Attack(weapon, defender) ? "#t".Eval() : "#f".Eval();
    }
    
    /// <summary>
    /// (kern-char-get-species char)
    /// </summary>
    public static object CharacterGetSpecies(object charObj)
    {
        // Handle apply wrapping.
        if (charObj is object[] args && args.Length >= 1)
        {
            charObj = args[0];
        }
        
        if (charObj is Character character)
        {
            return character.Species;  // Return the Species object directly.
        }

        if (charObj is Being being)
        {
            return being.Species;
        }
        
        // Try tag resolution.
        string tag = ToTag(charObj);
        if (!string.IsNullOrEmpty(tag))
        {
            var resolved = Phantasma.GetRegisteredObject(tag);
            if (resolved is Character resolvedCh)
                return resolvedCh.Species;
            if (resolved is Being resolvedB)
                return resolvedB.Species;
        }
        
        Console.WriteLine($"[kern-char-get-species] Invalid character: {charObj?.GetType().Name}");
        return "#f".Eval();
    }

    /// <summary>
    /// (kern-char-is-asleep? character)
    /// Returns #t if the character is sleeping, #f otherwise.
    /// </summary>
    public static object CharacterIsAsleep(object character)
    {
        // Handle variadic array wrapper from IronScheme.
        if (character is object[] arr && arr.Length > 0)
            character = arr[0];
        
        if (character is Being being)
        {
            return being.IsAsleep ? "#t".Eval() : "#f".Eval();
        }
        
        // Try tag resolution.
        string tag = ToTag(character);
        if (!string.IsNullOrEmpty(tag))
        {
            var resolved = Phantasma.GetRegisteredObject(tag);
            if (resolved is Being resolvedBeing)
                return resolvedBeing.IsAsleep ? "#t".Eval() : "#f".Eval();
        }
        
        Console.WriteLine($"[ERROR] kern-char-is-asleep?: not a being (got {character?.GetType().Name ?? "null"})");
        return "#f".Eval();
    }

    /// <summary>
    /// (kern-char-set-sleep character bool)
    /// Sets or clears the sleeping state.
    /// </summary>
    public static object CharacterSetSleep(object character, object value)
    {
        if (character is object[] arr && arr.Length >= 2)
        {
            character = arr[0];
            value = arr[1];
        }
        
        if (character is Being being)
        {
            // Determine boolean value from Scheme #t/#f/int.
            bool shouldSleep = !(value is false || 
                                 IsNil(value) || 
                                 (value is int i && i == 0));
            
            being.IsAsleep = shouldSleep;
            
            if (shouldSleep)
                Console.WriteLine($"{being.GetName()} falls asleep!");
            else
                Console.WriteLine($"{being.GetName()} wakes up!");
            
            return "#t".Eval();
        }
        
        Console.WriteLine("[ERROR] kern-char-set-sleep: not a being");
        return "#f".Eval();
    }
}
