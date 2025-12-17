using System;
using System.Collections.Generic;
using IronScheme;
using IronScheme.Runtime;

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
            return false;
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
    
        return true;
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
            return Builtins.Unspecified;
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
    public static object CharacterArmSelf(object character)
    {
        if (character is not Character ch)
        {
            Console.WriteLine("[ERROR] kern-char-arm-self: not a character");
            return Builtins.Unspecified;
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
            return Builtins.Unspecified;
        }
        
        var container = ch.GetInventoryContainer();
        if (container == null)
            return Builtins.Unspecified;
        
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
            return false;
        }
        
        if (weapon is not ArmsType arms)
        {
            Console.WriteLine("[ERROR] kern-char-has-ammo?: not an arms type");
            return false;
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
            return false;
        }
        
        if (armsType is not ArmsType arms)
        {
            Console.WriteLine("[ERROR] kern-char-ready: not an arms type");
            return false;
        }
        
        var result = ch.Ready(arms);
        return result == Character.ReadyResult.Readied;
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
            return false;
        }
        
        if (armsType is not ArmsType arms)
        {
            Console.WriteLine("[ERROR] kern-char-unready: not an arms type");
            return false;
        }
        
        return ch.Unready(arms);
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
            return null;
        }
    
        ch.Kill();
        return null;
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
        return null;
    }
}
