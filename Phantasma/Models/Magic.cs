using System;
using System.Collections.Generic;
using System.Linq;
using IronScheme;
using IronScheme.Runtime;

namespace Phantasma.Models;

/// <summary>
/// Magic system manager.
/// Handles spell casting, reagent mixing, and magic effects.
/// </summary>
public struct Magic
{
    // Spell word dictionary (syllable -> word) - STATIC/GLOBAL
    // These are the same for all sessions (like "An", "Bet", "Corp")
    private static Dictionary<char, string> globalSpellWords = new();
        
    // For enumeration (MixReagents needs to iterate all spells)
    private static HashSet<SpellType> allSpells = new();
    
    public Magic()
    {
    }
    
    /// <summary>
    /// Register a spell word (syllable) globally.
    /// Static so all sessions share the same spell words.
    /// </summary>
    public static void AddWordGlobal(char letter, string word)
    {
        globalSpellWords[char.ToUpper(letter)] = word;
    }
    
    /// <summary>
    /// Get spell word by first letter.
    /// </summary>
    public static string? GetWordGlobal(char letter)
    {
        return globalSpellWords.TryGetValue(char.ToUpper(letter), out var word) ? word : null;
    }

    public static void RegisterSpellForEnumeration(SpellType spell)
    {
        allSpells.Add(spell);
    }

    public static IEnumerable<SpellType> GetAllSpellsGlobal()
    {
        return allSpells;
    }
    
    /// <summary>
    /// Cast a spell.
    /// Returns true if spell was cast successfully.
    /// Static method - works with any character/spell regardless of session.
    /// </summary>
    public static bool CastSpell(Character caster, SpellType spell, object? target)
    {
        // Check if caster knows the spell
        if (!spell.IsKnownBy(caster))
        {
            Console.WriteLine($"{caster.Name} doesn't know {spell.Name}!");
            return false;
        }
        
        // Check mana cost
        if (!spell.CanAfford(caster))
        {
            Console.WriteLine($"{caster.Name} needs {spell.ManaCost} MP but only has {caster.MP}!");
            return false;
        }
        
        // Check reagents
        if (!spell.HasReagents(caster))
        {
            Console.WriteLine($"{caster.Name} doesn't have the required reagents!");
            return false;
        }
        
        // Validate target based on spell requirements
        if (!ValidateTarget(caster, spell, target))
        {
            return false;
        }
        
        // Check range
        if (!CheckRange(caster, spell, target))
        {
            Console.WriteLine($"Target is out of range! (max: {spell.Range})");
            return false;
        }
        
        // Check line of sight
        if (spell.RequiresLineOfSight && !CheckLineOfSight(caster, target))
        {
            Console.WriteLine($"No line of sight to target!");
            return false;
        }
        
        // Consume resources
        caster.MP -= spell.ManaCost;
        spell.ConsumeReagents(caster);
        
        // Apply spell effect
        bool success = ApplySpellEffect(caster, spell, target);
        
        if (success)
        {
            Console.WriteLine($"{caster.Name} casts {spell.Name}!");
        }
        
        return success;
    }
    
    /// <summary>
    /// Validate spell target.
    /// </summary>
    private static bool ValidateTarget(Character caster, SpellType spell, object? target)
    {
        // Self-cast spell
        if (spell.Range == 0)
        {
            if (target != null && target != caster)
            {
                Console.WriteLine($"{spell.Name} can only be cast on self!");
                return false;
            }
            return true;
        }
        
        // Empty tile target
        if (target == null)
        {
            if (!spell.CanTargetEmpty)
            {
                Console.WriteLine($"{spell.Name} requires a target!");
                return false;
            }
            return true;
        }
        
        // Character target
        if (target is Character targetChar)
        {
            // Check if targeting self
            if (targetChar == caster)
            {
                if (!spell.CanTargetSelf)
                {
                    Console.WriteLine($"{spell.Name} cannot be cast on self!");
                    return false;
                }
                return true;
            }
            
            // Check ally/enemy restrictions
            bool isAlly = caster.GetCurrentFaction() == targetChar.GetCurrentFaction();
            bool isEnemy = caster.GetCurrentFaction() != targetChar.GetCurrentFaction();
            
            if (isAlly && !spell.CanTargetAlly)
            {
                Console.WriteLine($"{spell.Name} cannot target allies!");
                return false;
            }
            
            if (isEnemy && !spell.CanTargetEnemy)
            {
                Console.WriteLine($"{spell.Name} cannot target enemies!");
                return false;
            }
            
            return true;
        }
        
        // Unknown target type
        Console.WriteLine($"Invalid target for {spell.Name}!");
        return false;
    }
    
    /// <summary>
    /// Check if target is in range.
    /// </summary>
    private static bool CheckRange(Character caster, SpellType spell, object? target)
    {
        // Unlimited range
        if (spell.Range < 0)
            return true;
        
        // Self-cast
        if (spell.Range == 0)
            return target == null || target == caster;
        
        // Check distance to target
        if (target is Object targetObj)
        {
            var casterPos = caster.GetPosition();
            var targetPos = targetObj.GetPosition();
            
            if (casterPos == null || targetPos == null)
                return false;
            
            if (casterPos.Place != targetPos.Place)
                return false;
            
            int dx = Math.Abs(casterPos.X - targetPos.X);
            int dy = Math.Abs(casterPos.Y - targetPos.Y);
            int distance = Math.Max(dx, dy); // Chebyshev distance
            
            return distance <= spell.Range;
        }
        
        return false;
    }
    
    /// <summary>
    /// Check line of sight to target.
    /// </summary>
    private static bool CheckLineOfSight(Character caster, object? target)
    {
        if (target == null || target == caster)
            return true;
        
        if (target is Object targetObj)
        {
            var casterPos = caster.GetPosition();
            var targetPos = targetObj.GetPosition();
            
            if (casterPos == null || targetPos == null)
                return false;
            
            if (casterPos.Place != targetPos.Place)
                return false;
            
            var place = casterPos.Place;
            
            // Use Phantasma's LineOfSight system (shadowcasting from Task 8)
            // Create a LOS grid centered on the caster
            int losSize = 39; // Standard visibility mask size
            var los = new LineOfSight(losSize, losSize);
            
            int halfSize = losSize / 2;
            int startX = casterPos.X - halfSize;
            int startY = casterPos.Y - halfSize;
            
            // Fill alpha buffer with terrain transparency
            for (int y = 0; y < losSize; y++)
            {
                for (int x = 0; x < losSize; x++)
                {
                    int mapX = startX + x;
                    int mapY = startY + y;
                    
                    // GetVisibility returns 0 for opaque, 12 for transparent
                    if (mapX >= 0 && mapX < place.Width && mapY >= 0 && mapY < place.Height)
                    {
                        los.Alpha[y * losSize + x] = place.GetVisibility(mapX, mapY);
                    }
                    else
                    {
                        los.Alpha[y * losSize + x] = 0; // Out of bounds = opaque
                    }
                }
            }
            
            // Compute visibility from center (caster position)
            los.Compute();
            
            // Convert target position to LOS grid coordinates
            int targetLosX = targetPos.X - startX;
            int targetLosY = targetPos.Y - startY;
            
            // Check if target is in the LOS grid and visible
            if (targetLosX >= 0 && targetLosX < losSize && targetLosY >= 0 && targetLosY < losSize)
            {
                int targetIndex = targetLosY * losSize + targetLosX;
                return los.VisibilityMask[targetIndex] > 0;
            }
            
            return false; // Target outside LOS range
        }
        
        return false;
    }
    
    /// <summary>
    /// Apply the spell's effect.
    /// Calls the spell's Scheme closure if present.
    /// </summary>
    private static bool ApplySpellEffect(Character caster, SpellType spell, object? target)
    {
        if (spell.Effect == null)
        {
            Console.WriteLine($"[WARNING] Spell {spell.Name} has no effect defined!");
            return false;
        }
        
        // Call Scheme closure: (effect caster target).
        // The closure should return #t for success, #f for failure.
        if (spell.Effect is Callable callable)
        {
            var result = callable.Call(caster, target);
            return result != null && result != "#f".Eval();
        }
        
        // Return false because the closure did not return true.
        return false;
    }
    
    /// <summary>
    /// Mix reagents to identify a spell.
    /// Used for Ultima-style magic where players mix reagents to discover spells.
    /// </summary>
    public SpellType? MixReagents(Character character, List<ReagentType> reagents)
    {
        // Find spell that matches these reagents from global registry
        var matchingSpell = GetAllSpellsGlobal().FirstOrDefault(spell =>
        {
            if (spell.RequiredReagents.Count != reagents.Count)
                return false;
            
            foreach (var reagent in reagents)
            {
                if (!spell.RequiredReagents.ContainsKey(reagent))
                    return false;
            }
            
            return true;
        });
        
        if (matchingSpell != null)
        {
            Console.WriteLine($"Mixed reagents form the spell: {matchingSpell.Name}!");
            
            // Add spell to character's known spells
            if (!matchingSpell.IsKnownBy(character))
            {
                character.LearnSpell(matchingSpell);
                Console.WriteLine($"{character.Name} learned {matchingSpell.Name}!");
            }
        }
        else
        {
            Console.WriteLine("The reagents fizzle uselessly...");
        }
        
        return matchingSpell;
    }
}