using System;
using System.Collections.Generic;
using System.Linq;

namespace Phantasma.Models;

/// <summary>
/// Command.Magic - Magic and Spell Commands
/// 
/// Commands for magic system:
/// - CastSpell: Cast spells (learned or mixed)
/// - MixReagents: Mix reagents to create spells
/// - Yuse: Use special abilities/skills
/// </summary>
public partial class Command
{
    // ===================================================================
    // CAST SPELL COMMAND - Cast magic spells
    // ===================================================================
    
    /// <summary>
    /// CastSpell Command - cast a magic spell.
    /// Mirrors Nazghul's cmdCastSpell().
    /// 
    /// Flow:
    /// 1. Select caster (party member)
    /// 2. Show castable spells
    /// 3. Select spell
    /// 4. If ranged, prompt for target
    /// 5. Cast spell (validates mana, reagents, range, LOS)
    /// </summary>
    /// <param name="pc">Character casting, or null to prompt</param>
    /// <returns>True if spell was cast successfully</returns>
    public bool CastSpell(Character? pc = null)
    {
        if (session?.Party == null)
            return false;
        
        ShowPrompt("Cast-");
        
        // Select caster if not provided
        if (pc == null)
        {
            pc = SelectPartyMember();
            if (pc == null)
            {
                ClearPrompt();
                return false;
            }
        }
        
        ShowPrompt($"Cast [{pc.GetName()}]-");
        
        // Get spells this character can cast
        var castableSpells = pc.GetCastableSpells().ToList();
        
        if (castableSpells.Count == 0)
        {
            Log($"{pc.GetName()} knows no spells!");
            ClearPrompt();
            return false;
        }
        
        // Show spell menu
        Log($"Castable spells for {pc.GetName()}:");
        for (int i = 0; i < castableSpells.Count; i++)
        {
            var spell = castableSpells[i];
            string manaInfo = spell.CanAfford(pc) ? $"{spell.ManaCost} MP" : $"{spell.ManaCost} MP (NOT ENOUGH!)";
            string reagentInfo = spell.HasReagents(pc) ? "" : " (missing reagents!)";
            Log($"  {i + 1}. {spell.Name} - {manaInfo}{reagentInfo}");
        }
        
        // Prompt for spell selection
        ShowPrompt($"Cast [{pc.GetName()}]-Which spell? (1-{castableSpells.Count}, ESC=cancel): ");
        
        // TODO: Replace with actual menu selection when UI is ready
        // For now, just auto-select first spell for testing
        int spellIndex = 0; // Would come from user input
        
        if (spellIndex < 0 || spellIndex >= castableSpells.Count)
        {
            Log("Cancelled.");
            ClearPrompt();
            return false;
        }
        
        var selectedSpell = castableSpells[spellIndex];
        ShowPrompt($"Cast [{pc.GetName()}]-{selectedSpell.Name}-");
        
        // Determine target based on spell type
        object? target = null;
        
        if (selectedSpell.CanTargetSelf && !selectedSpell.CanTargetAlly && !selectedSpell.CanTargetEnemy)
        {
            // Self-only spell
            target = pc;
        }
        else if (selectedSpell.Range == 0)
        {
            // Melee range - prompt for direction
            ShowPrompt($"Cast [{pc.GetName()}]-{selectedSpell.Name}-Direction: ");
            var dir = PromptForDirection();
            if (dir == null)
            {
                ClearPrompt();
                return false;
            }
            
            int targetX = pc.GetX() + Common.DirectionToDx(dir);
            int targetY = pc.GetY() + Common.DirectionToDy(dir);
            target = session.CurrentPlace?.GetBeingAt(targetX, targetY);
            
            if (target == null && !selectedSpell.CanTargetEmpty)
            {
                Log("No target there!");
                ClearPrompt();
                return false;
            }
        }
        else
        {
            // Ranged spell - use targeting
            ShowPrompt($"Cast [{pc.GetName()}]-{selectedSpell.Name}-Select target (arrows to move, Enter to select, ESC to cancel)");
            target = PromptForTarget(pc.GetX(), pc.GetY(), selectedSpell.Range);
            
            if (target == null && !selectedSpell.CanTargetEmpty)
            {
                Log("Cancelled.");
                ClearPrompt();
                return false;
            }
        }
        
        // Cast the spell
        bool success = Magic.CastSpell(pc, selectedSpell, target);
        
        if (success)
        {
            Log($"{pc.GetName()} casts {selectedSpell.Name}!");
        }
        else
        {
            Log($"{pc.GetName()} failed to cast {selectedSpell.Name}.");
        }
        
        ClearPrompt();
        return success;
    }
    
    // ===================================================================
    // MIX REAGENTS COMMAND - Create spell mixtures
    // ===================================================================
    
    /// <summary>
    /// MixReagents Command - mix reagents to discover spells (Ultima-style).
    /// Mirrors Nazghul's cmdMixReagents().
    /// 
    /// Flow:
    /// 1. Select character
    /// 2. Show available reagents
    /// 3. Select reagents to mix
    /// 4. Try to find matching spell
    /// 5. If found, learn spell; if not, reagents are wasted
    /// </summary>
    /// <returns>True if mixing completed (even if failed)</returns>
    public bool MixReagents()
    {
        if (session?.Party == null)
            return false;
        
        ShowPrompt("Mix-");
        
        // Select character
        var character = SelectPartyMember();
        if (character == null)
        {
            ClearPrompt();
            return false;
        }
        
        var inventory = character.GetInventoryContainer();
        if (inventory == null)
        {
            Log("No inventory!");
            ClearPrompt();
            return false;
        }
        
        // Collect all reagent types from inventory
        var reagentList = new List<ReagentType>();
        foreach (var item in inventory.GetContents())
        {
            if (item.Type is ReagentType rt && !reagentList.Contains(rt))
            {
                reagentList.Add(rt);
            }
        }
        
        if (reagentList.Count == 0)
        {
            Log($"{character.GetName()} has no reagents!");
            ClearPrompt();
            return false;
        }
        
        // Show available reagents
        Log($"Reagents available:");
        for (int i = 0; i < reagentList.Count; i++)
        {
            Log($"  {reagentList[i].DisplayChar}. {reagentList[i].Name}");
        }
        
        // For now, try mixing all available reagent types
        // TODO: Implement reagent selection UI
        Log($"Mixing all {reagentList.Count} reagent types...");
        
        Magic.MixReagents(character, reagentList);
        
        ClearPrompt();
        return true;
    }
    
    // ===================================================================
    // YUSE COMMAND - Use special abilities/skills
    // ===================================================================
    
    /// <summary>
    /// Yuse Command - use special abilities or skills.
    /// Mirrors Nazghul's cmdYuse().
    /// 
    /// This is for character abilities that aren't spells or items.
    /// Examples: racial abilities, class skills, special powers.
    /// </summary>
    /// <param name="pc">Character using ability, or null to prompt</param>
    /// <returns>True if ability was used successfully</returns>
    public bool Yuse(Character? pc = null)
    {
        // TODO: Implement in Task 30 (Skill System) or later
        Log("Yuse (special abilities) command not yet implemented");
        Log("Will be implemented with the Skill System");
        return false;
    }
    
    // ===================================================================
    // TARGETING HELPERS
    // ===================================================================
    
    /// <summary>
    /// Prompt for a target using a targeting cursor.
    /// Returns the target object, or null if cancelled/nothing selected.
    /// </summary>
    /// <param name="startX">Starting X coordinate (usually caster position)</param>
    /// <param name="startY">Starting Y coordinate (usually caster position)</param>
    /// <param name="maxRange">Maximum range from start position (0 = unlimited)</param>
    /// <returns>Selected target object, or null if cancelled</returns>
    private object? PromptForTarget(int startX, int startY, int maxRange)
    {
        // TODO: Implement proper targeting cursor with visual feedback
        // For now, use direction-based targeting for adjacent targets
        
        if (maxRange <= 1)
        {
            // For range 1, just use direction
            var dir = PromptForDirection();
            if (dir == null)
                return null;
            
            int targetX = startX + Common.DirectionToDx(dir);
            int targetY = startY + Common.DirectionToDy(dir);
            return session.CurrentPlace?.GetBeingAt(targetX, targetY);
        }
        
        // For longer ranges, we need a proper targeting UI
        // For now, log a message and return null
        Log("Long-range targeting not yet implemented - selecting nearest enemy");
        
        // Find nearest enemy within range
        if (session.CurrentPlace != null)
        {
            var player = session.Player;
            if (player != null)
            {
                var enemies = session.CurrentPlace.GetAllBeings()
                    .Where(b => b != player && IsHostile(b, player))
                    .OrderBy(b => GetDistance(startX, startY, b.GetX(), b.GetY()))
                    .ToList();
                
                foreach (var enemy in enemies)
                {
                    int dist = GetDistance(startX, startY, enemy.GetX(), enemy.GetY());
                    if (maxRange == 0 || dist <= maxRange)
                    {
                        return enemy;
                    }
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Check if being is hostile to another.
    /// </summary>
    private bool IsHostile(Being a, Being b)
    {
        // TODO: Implement proper faction/hostility system
        // For now, simple check: NPCs vs player party
        if (a is Character && b is Character)
            return false; // Characters in same party aren't hostile
        
        return true; // NPCs are hostile to player
    }
    
    /// <summary>
    /// Get Chebyshev distance (max of dx, dy) between two points.
    /// </summary>
    private int GetDistance(int x1, int y1, int x2, int y2)
    {
        return Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1));
    }
}
