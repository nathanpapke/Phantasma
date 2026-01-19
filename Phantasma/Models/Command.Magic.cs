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
    public void CastSpell(Character? pc = null)
    {
        ShowPrompt("Cast-");
        
        // Select caster if not provided.
        if (pc == null)
        {
            pc = SelectPartyMember();
            if (pc == null)
            {
                ShowPrompt("Cast-none!");
                return;
            }
        }
        
        ShowPrompt($"Cast [{pc.GetName()}]-");
        
        // Check if character can cast spells.
        if (pc.GetMaxMana() <= 0)
        {
            Log($"{pc.GetName()} cannot cast spells!");
            ClearPrompt();
            return;
        }
        
        // Get list of known spells.
        var knownSpells = pc.GetCastableSpells();
        if (knownSpells == null || knownSpells.Count() == 0)
        {
            Log($"{pc.GetName()} knows no spells!");
            ClearPrompt();
            return;
        }
        
        // Filter to castable spells (have mana and reagents).
        var castableSpells = new List<SpellType>();
        foreach (var spell in knownSpells)
        {
            if (pc.MP >= spell.ManaCost)
            {
                castableSpells.Add(spell);
            }
        }
        
        if (castableSpells.Count == 0)
        {
            Log($"{pc.GetName()} has no castable spells (not enough mana)!");
            ClearPrompt();
            return;
        }
        
        // Show available spells
        Log($"=== Spells ({pc.MP}/{pc.GetMaxMana()} MP) ===");
        for (int i = 0; i < castableSpells.Count; i++)
        {
            var spell = castableSpells[i];
            Log($"  {i + 1}. {spell.Name} ({spell.ManaCost} MP)");
        }
        
        // TODO: Implement spell selection UI
        // For now, auto-cast first spell for testing
        Log("(Spell selection not implemented - casting first spell)");
        
        var selectedSpell = castableSpells[0];
        CastSelectedSpell(pc, selectedSpell);
    }
    
    /// <summary>
    /// Cast a selected spell, handling targeting if needed.
    /// </summary>
    private void CastSelectedSpell(Character caster, SpellType spell)
    {
        ShowPrompt($"Cast-{caster.GetName()}-{spell.Name}-");
        
        // Determine target based on spell type.
        if (spell.CanTargetSelf && !spell.CanTargetAlly && !spell.CanTargetEnemy)
        {
            // Self-only spell
            ExecuteSpell(caster, spell, caster);
        }
        else if (spell.Range == 0)
        {
            // Melee range - need direction
            ShowPrompt($"Cast-{caster.GetName()}-{spell.Name}-<direction>");
            
            var c = caster;
            var s = spell;
            RequestDirection(dir => CompleteSpellCastDirection(c, s, dir));
        }
        else
        {
            // Ranged spell - use targeting
            ShowPrompt($"Cast-{caster.GetName()}-{spell.Name}-<select target>");
            
            var c = caster;
            var s = spell;
            
            // Note: BeginTargeting callback is (targetX, targetY, cancelled)
            session.BeginTargeting(
                caster.GetX(),
                caster.GetY(),
                spell.Range,
                caster.GetX(),
                caster.GetY(),
                (x, y, cancelled) => CompleteSpellCastTarget(c, s, !cancelled, x, y)
            );
        }
    }
    
    /// <summary>
    /// Complete spell cast after direction received.
    /// </summary>
    private void CompleteSpellCastDirection(Character caster, SpellType spell, Direction? dir)
    {
        if (dir == null)
        {
            ShowPrompt($"Cast-{caster.GetName()}-{spell.Name}-cancelled");
            return;
        }
        
        int dx = Common.DirectionToDx(dir.Value);
        int dy = Common.DirectionToDy(dir.Value);
        
        var place = caster.GetPlace();
        if (place == null)
        {
            return;
        }
        
        int targetX = caster.GetX() + dx;
        int targetY = caster.GetY() + dy;
        
        var target = place.GetBeingAt(targetX, targetY);
        
        if (target == null && !spell.CanTargetEmpty)
        {
            Log("No target there!");
            ClearPrompt();
            return;
        }
        
        ExecuteSpell(caster, spell, target);
    }
    
    /// <summary>
    /// Complete spell cast after targeting.
    /// Note: BeginTargeting automatically handles cleanup when callback is invoked.
    /// </summary>
    private void CompleteSpellCastTarget(Character caster, SpellType spell, 
        bool confirmed, int x, int y)
    {
        // Note: targeting cleanup (IsTargeting=false, PopKeyHandler) is done by Session.BeginTargeting
        
        if (!confirmed)
        {
            ShowPrompt($"Cast-{caster.GetName()}-{spell.Name}-cancelled");
            return;
        }
        
        var place = caster.GetPlace();
        var target = place?.GetBeingAt(x, y);
        
        if (target == null && !spell.CanTargetEmpty)
        {
            Log("No target there!");
            ClearPrompt();
            return;
        }
        
        ExecuteSpell(caster, spell, target);
    }
    
    /// <summary>
    /// Execute the spell effect.
    /// </summary>
    private void ExecuteSpell(Character caster, SpellType spell, object? target)
    {
        // Deduct mana.
        caster.MP -= spell.ManaCost;
        
        Log($"{caster.GetName()} casts {spell.Name}!");
        
        // Call the spell's effect handler.
        bool success = Magic.CastSpell(caster, spell, target);
        
        if (!success)
        {
            Log("The spell fizzles...");
        }
        
        ClearPrompt();
    }
    
    // ===================================================================
    // MIX REAGENTS COMMAND - Create spell mixtures
    // ===================================================================
    
    /// <summary>
    /// MixReagents Command - mix reagents to discover spells (Ultima-style).
    /// 
    /// Flow:
    /// 1. Select character
    /// 2. Show available reagents
    /// 3. Select reagents to mix
    /// 4. Try to find matching spell
    /// 5. If found, learn spell; if not, reagents are wasted
    /// </summary>
    /// <returns>True if mixing completed (even if failed)</returns>
    public void MixReagents()
    {
        if (session?.Party == null)
            return;
        
        ShowPrompt("Mix-");
        
        // Select character.
        var character = SelectPartyMember();
        if (character == null)
        {
            ShowPrompt("Mix-none!");
            return;
        }
        
        var inventory = character.GetInventoryContainer();
        if (inventory == null)
        {
            Log("No inventory!");
            ClearPrompt();
            return;
        }
        
        // Collect all reagent types from inventory.
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
            return;
        }
        
        // Show available reagents.
        Log($"Reagents available:");
        for (int i = 0; i < reagentList.Count; i++)
        {
            Log($"  {reagentList[i].DisplayChar}. {reagentList[i].Name}");
        }
        
        // For now, try mixing all available reagent types.
        // TODO: Implement reagent selection UI.
        Log($"Mixing all {reagentList.Count} reagent types...");
        
        Magic.MixReagents(character, reagentList);
        
        ClearPrompt();
    }
    
    // ===================================================================
    // YUSE COMMAND - Use special abilities/skills
    // ===================================================================
    
    /// <summary>
    /// Yuse Command - use special abilities or skills.
    /// 
    /// This is for character abilities that aren't spells or items.
    /// Examples: racial abilities, class skills, special powers.
    /// </summary>
    public void Yuse()
    {
        ShowPrompt("Yuse-");
        
        var pc = SelectPartyMember();
        if (pc == null)
        {
            ShowPrompt("Yuse-none!");
            return;
        }
        
        ShowPrompt($"Yuse-{pc.GetName()}-");
        
        // Get character's available abilities.
        var abilities = pc.Species.Spells;
        if (abilities == null || abilities.Length == 0)
        {
            Log($"{pc.GetName()} has no special abilities!");
            ClearPrompt();
            return;
        }
        
        // Show available abilities.
        Log("=== Special Abilities ===");
        for (int i = 0; i < abilities.Length; i++)
        {
            Log($"  {i + 1}. {abilities[i]}");
        }
        
        // TODO: Implement ability selection and execution
        Log("Yuse command not fully implemented yet");
        
        ClearPrompt();
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
