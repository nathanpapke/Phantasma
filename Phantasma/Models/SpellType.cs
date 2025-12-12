using System.Collections.Generic;
using System.Linq;

namespace Phantasma.Models;

/// <summary>
/// SpellType defines a spell template (like Fireball, Heal, etc).
/// Individual spell instances are cast from these templates.
/// </summary>
public class SpellType
{
    public string Tag { get; set; } = "";
    public string Name { get; set; } = "";
    public int Level { get; set; }  // Spell level (1-8)
    public int ManaCost { get; set; }
    
    /// <summary>
    /// ASCII character for display (e.g., '*' for magic missile).
    /// </summary>
    public char DisplayChar { get; set; } = '*';
    
    /// <summary>
    /// Sprite for visual effects (optional).
    /// </summary>
    public Sprite? Sprite { get; set; }
    
    /// <summary>
    /// Range in tiles (0 = self, -1 = unlimited).
    /// </summary>
    public int Range { get; set; }
    
    /// <summary>
    /// Can this spell target an empty tile?
    /// </summary>
    public bool CanTargetEmpty { get; set; }
    
    /// <summary>
    /// Can this spell target an ally?
    /// </summary>
    public bool CanTargetAlly { get; set; }
    
    /// <summary>
    /// Can this spell target an enemy?
    /// </summary>
    public bool CanTargetEnemy { get; set; }
    
    /// <summary>
    /// Can this spell target self?
    /// </summary>
    public bool CanTargetSelf { get; set; }
    
    /// <summary>
    /// Does this spell require line of sight?
    /// </summary>
    public bool RequiresLineOfSight { get; set; }
    
    /// <summary>
    /// Reagents required to cast this spell.
    /// Dictionary of ReagentType -> quantity needed.
    /// </summary>
    public Dictionary<ReagentType, int> RequiredReagents { get; set; } = new();
    
    /// <summary>
    /// Scheme closure that implements the spell's effect.
    /// Called when the spell is cast.
    /// Signature: (lambda (caster target) ...)
    /// </summary>
    public object? Effect { get; set; }
    
    /// <summary>
    /// Check if a character knows this spell.
    /// </summary>
    public bool IsKnownBy(Character character)
    {
        return character.KnownSpells.Contains(this);
    }
    
    /// <summary>
    /// Check if a character can afford to cast this spell.
    /// </summary>
    public bool CanAfford(Character character)
    {
        return character.MP >= ManaCost;
    }
    
    /// <summary>
    /// Check if a character has the required reagents.
    /// </summary>
    public bool HasReagents(Character character)
    {
        if (RequiredReagents.Count == 0)
            return true;
        
        var inventory = character.GetInventoryContainer();
        if (inventory == null)
            return false;
        
        // Count reagents by type.
        var available = new Dictionary<ReagentType, int>();
        foreach (var item in inventory.GetContents())
        {
            var rt = item.Type as ReagentType;
            if (rt != null)
            {
                if (!available.ContainsKey(rt))
                    available[rt] = 0;
                available[rt] += item.Quantity;
            }
        }
        
        // Check if we have enough of each required reagent.
        foreach (var required in RequiredReagents)
        {
            int have = available.TryGetValue(required.Key, out int count) ? count : 0;
            if (have < required.Value)
                return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Consume reagents from character's inventory.
    /// </summary>
    public void ConsumeReagents(Character character)
    {
        var inventory = character.GetInventoryContainer();
        if (inventory == null)
            return;
        
        foreach (var required in RequiredReagents)
        {
            int remaining = required.Value;
            var reagentType = required.Key;
            
            // Find and remove reagent items.
            var itemsToCheck = inventory.Contents;
            foreach (var item in itemsToCheck)
            {
                if (remaining <= 0)
                    break;
                
                if (item.Type is ReagentType rt && rt.Equals(reagentType))
                {
                    int itemQty = item.Quantity;
                    
                    if (itemQty <= remaining)
                    {
                        // Remove entire stack.
                        inventory.RemoveItem(item);
                        remaining -= itemQty;
                    }
                    else
                    {
                        // Reduce stack.
                        item.Quantity -= remaining;  //check if works
                        remaining = 0;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Get a description of what this spell does.
    /// </summary>
    public string GetDescription()
    {
        var parts = new List<string>();
        
        parts.Add($"Level {Level} spell");
        parts.Add($"Costs {ManaCost} MP");
        
        if (Range == 0)
            parts.Add("Self only");
        else if (Range < 0)
            parts.Add("Unlimited range");
        else
            parts.Add($"Range: {Range} tiles");
        
        if (RequiredReagents.Count > 0)
        {
            var reagentList = string.Join(", ", 
                RequiredReagents.Select(r => $"{r.Value}x {r.Key.Name}"));
            parts.Add($"Reagents: {reagentList}");
        }
        
        return string.Join(", ", parts);
    }
}