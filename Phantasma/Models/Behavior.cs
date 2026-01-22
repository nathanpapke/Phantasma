using System;
using System.Collections.Generic;
using System.Linq;
using IronScheme.Runtime;

namespace Phantasma.Models;

/// <summary>
/// NPC AI Controller - This emulates Nazghul's control logic for NPC behavior.
/// 
/// This implements the default AI behavior for NPCs:
/// - Target selection (find nearest hostile)
/// - Attack if in range
/// - Pathfind toward targets
/// - Wander if no targets
/// </summary>
public class Behavior
{
    private static Random random = new Random();
    
    // ===================================================================
    // MAIN ENTRY POINT - Called by Session.HandleOtherBeings().
    // ===================================================================
    
    /// <summary>
    /// Execute AI for a character. This is the main entry point.
    /// </summary>
    public static void Execute(Character character)
    {
        if (character == null || character.IsDead)
            return;
        
        // If character has a custom AI closure, execute that instead.
        if (character.HasAI)
        {
            try
            {
                if (character.AIBehavior is Callable callable)
                {
                    callable.Call(character);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NpcAI] Custom AI error for {character.GetName()}: {ex.Message}");
            }
            return;
        }
        
        // Fleeing overrides normal activity.
        if (character.IsFleeing)
        {
            Flee(character);
            return;
        }
        
        // Default behavior based on activity.
        switch (character.CurrentActivity)
        {
            case Activity.Commuting:
                // TODO: Implement commute behavior
                Idle(character);
                break;
            case Activity.Eating:
            case Activity.Sleeping:
                // Do nothing when eating/sleeping
                character.EndTurn();
                break;
            default:
                Idle(character);
                break;
        }
    }
    
    // ===================================================================
    // IDLE BEHAVIOR - Find targets, attack, or wander
    // ===================================================================
    
    /// <summary>
    /// Default idle behavior - look for enemies, attack if possible, else wander.
    /// </summary>
    private static void Idle(Character character)
    {
        // Find a target.
        var target = SelectTarget(character);
        
        if (target == null)
        {
            // No targets - just wander.
            Wander(character);
            return;
        }
        
        // If can't see target, try to pathfind to remembered location.
        if (!character.CanSee(target))
        {
            if (!PathfindToward(character, target))
            {
                Wander(character);
            }
            return;
        }
        
        // Can see target - try to attack.
        if (!AttackTarget(character, target))
        {
            // Couldn't attack (out of range?) - move toward target.
            if (!PathfindToward(character, target))
            {
                Wander(character);
            }
        }
    }
    
    // ===================================================================
    // TARGET SELECTION - Find the closest hostile
    // ===================================================================
    
    /// <summary>
    /// Select a target using a heuristic (closest hostile).
    /// </summary>
    public static Character? SelectTarget(Character attacker)
    {
        var place = attacker.GetPlace();
        if (place == null)
            return null;
        
        Character? bestTarget = null;
        int bestDistance = int.MaxValue;
        
        // Search all beings in the place.
        foreach (var obj in place.GetAllBeings())
        {
            if (!IsValidTarget(attacker, obj))
                continue;
            
            // Compute distance.
            int distance = place.GetFlyingDistance(
                attacker.GetX(), attacker.GetY(),
                obj.GetX(), obj.GetY());
            
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = obj as Character;
            }
        }
        
        // Remember the target.
        if (bestTarget != null)
        {
            attacker.SetAttackTarget(bestTarget);
            return bestTarget;
        }
        
        // Try the old target if still valid.
        var oldTarget = attacker.GetAttackTarget() as Character;
        if (oldTarget != null && IsValidTarget(attacker, oldTarget))
        {
            return oldTarget;
        }
        
        // Set no valid targets.
        attacker.SetAttackTarget(null);
        return null;
    }
    
    /// <summary>
    /// Check if an object is a valid target for the attacker.
    /// </summary>
    private static bool IsValidTarget(Character attacker, Being? obj)
    {
        // Skip null.
        if (obj == null)
            return false;
        
        // Skip non-characters.
        if (obj is not Character target)
            return false;
        
        // Skip self.
        if (obj == attacker)
            return false;
        
        // Skip dead beings.
        if (target.IsDead)
            return false;
        
        // Skip non-hostiles.
        if (!AreHostile(attacker, target))
            return false;
        
        // Skip non-visible objects.
        if (!attacker.CanSee(target))
            return false;
        
        // Skip off-map beings.
        if (!target.IsOnMap())
            return false;
        
        return true;
    }
    
    // ===================================================================
    // HOSTILITY CHECK
    // ===================================================================
    
    /// <summary>
    /// Check if two beings are hostile to each other.
    /// Uses faction system and diplomacy table.
    /// </summary>
    public static bool AreHostile(Being a, Being b)
    {
        // Get factions.
        int factionA = a.GetCurrentFaction();
        int factionB = b.GetCurrentFaction();
        
        // Same faction = not hostile.
        if (factionA == factionB)
            return false;
        
        // Check diplomacy table.
        var session = Phantasma.MainSession;
        if (session?.DiplomacyTable != null)
        {
            int relation = session.DiplomacyTable.Get(factionA, factionB);
            return relation < 0;  // Negative = hostile
        }
        
        // Default: different factions with no diplomacy = hostile
        // Exception: faction 0 (player) vs faction > 0 (NPCs) check.
        // For simplicity, assume player faction (usually 0) is hostile to monsters.
        
        // Player faction is typically 0, friendly NPCs might share it.
        // Hostile monsters typically have different factions.
        return factionA != factionB && (factionA == 0 || factionB == 0);
    }
    
    // ===================================================================
    // ATTACK LOGIC
    // ===================================================================
    
    /// <summary>
    /// Attempt to attack a target with available weapons.
    /// </summary>
    private static bool AttackTarget(Character attacker, Character target)
    {
        var place = attacker.GetPlace();
        if (place == null)
            return false;
        
        int distance = place.GetFlyingDistance(
            attacker.GetX(), attacker.GetY(),
            target.GetX(), target.GetY());
        
        bool attacked = false;
        
        // Try each weapon.
        foreach (var weapon in attacker.GetReadiedWeapons())
        {
            if (weapon == null)
                continue;
            
            // Check range.
            if (distance > weapon.Range)
                continue;
            
            // Check ammo.
            if (!attacker.HasAmmo(weapon))
                continue;
            
            // Missile weapons are blocked at melee range.
            if (distance <= 1 && weapon.IsThrown)
                continue;
            
            // Execute the attack.
            Attack(attacker, weapon, target);
            attacked = true;
            
            // Stop if target is dead.
            if (target.IsDead)
                break;
            
            // Stop if out of action points.
            if (attacker.ActionPoints <= 0)
                break;
        }
        
        // Rearm if needed.
        if (attacker.NeedsToRearm())
        {
            attacker.ArmThyself();
        }
        
        return attacked;
    }
    
    /// <summary>
    /// Execute an attack with a weapon.
    /// </summary>
    public static void Attack(Character attacker, ArmsType weapon, Character target)
    {
        var session = Phantasma.MainSession;
        
        // Log attack.
        string msg = $"{attacker.GetName()}: {weapon.Name} - {target.GetName()} ";
        Console.Write(msg);
        session?.LogMessage(msg);
        
        // Fire the weapon (handles missile animation).
        bool miss = !weapon.Fire(target, attacker.GetX(), attacker.GetY());
        
        // Consume action points and ammo.
        attacker.DecreaseActionPoints(weapon.RequiredActionPoints);
        attacker.UseAmmo(weapon);
        
        if (miss)
        {
            Console.WriteLine("missed!");
            session?.LogMessage("missed!");
            return;
        }
        
        // Roll to hit.
        int hit = Dice.Roll("1d20") + Dice.Roll(weapon.ToHitDice);
        int def = target.GetDefend();
        
        Console.WriteLine($"[Hit: {hit} vs Def: {def}]");
        
        if (hit < def)
        {
            Console.WriteLine("barely scratched!");
            session?.LogMessage("barely scratched!");
            return;
        }
        
        // Roll for damage.
        int damage = Dice.Roll(weapon.DamageDice);
        int armor = target.GetArmor();
        damage = Math.Max(0, damage - armor);
        
        Console.WriteLine($"[Damage: {damage}]");
        
        // Apply damage.
        target.Damage(damage);
        
        string woundMsg = $"{target.GetWoundDescription()}!";
        Console.WriteLine(woundMsg);
        session?.LogMessage(woundMsg);
        
        // Award XP if killed.
        if (target.IsDead)
        {
            int xp = target.GetExperienceValue();
            attacker.AddExperience(xp);
            session?.LogMessage($"{attacker.GetName()} gains {xp} XP!");
        }
    }
    
    // ===================================================================
    // MOVEMENT
    // ===================================================================
    
    /// <summary>
    /// Move toward a target using simple pathfinding.
    /// </summary>
    private static bool PathfindToward(Character source, Being target)
    {
        var place = source.GetPlace();
        if (place == null)
            return false;
        
        int targetX = target.GetX();
        int targetY = target.GetY();
        int sourceX = source.GetX();
        int sourceY = source.GetY();
        
        // Simple pathfinding: move in the general direction.
        int dx = Math.Sign(targetX - sourceX);
        int dy = Math.Sign(targetY - sourceY);
        
        // Try direct path first.
        if (dx != 0 || dy != 0)
        {
            if (TryMove(source, dx, dy))
                return true;
            
            // Try just horizontal.
            if (dx != 0 && TryMove(source, dx, 0))
                return true;
            
            // Try just vertical.
            if (dy != 0 && TryMove(source, 0, dy))
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Try to move in a direction, checking passability.
    /// </summary>
    private static bool TryMove(Character character, int dx, int dy)
    {
        var place = character.GetPlace();
        if (place == null)
            return false;
        
        int newX = character.GetX() + dx;
        int newY = character.GetY() + dy;
        
        // Check bounds.
        if (place.IsOffMap(newX, newY))
            return false;
        
        // Check passability.
        if (!place.IsPassable(newX, newY, character))
            return false;
        
        // Check for hazards (NPCs avoid hazards when wandering).
        if (place.IsHazardous(newX, newY))
            return false;
        
        // Move.
        return character.Move(dx, dy);
    }
    
    /// <summary>
    /// Wander randomly.
    /// </summary>
    private static void Wander(Character character)
    {
        var place = character.GetPlace();
        if (place == null)
        {
            character.EndTurn();
            return;
        }
        
        // Roll for direction.
        int dx = random.Next(3) - 1;  // -1, 0, or 1
        int dy = 0;
        if (dx == 0)
            dy = random.Next(3) - 1;
        
        if (dx != 0 || dy != 0)
        {
            int newX = character.GetX() + dx;
            int newY = character.GetY() + dy;
            
            // Don't wander off map.
            if (place.IsOffMap(newX, newY))
            {
                character.EndTurn();
                return;
            }
            
            // Don't wander into hazards.
            if (place.IsHazardous(newX, newY))
            {
                character.EndTurn();
                return;
            }
            
            // Try to move.
            character.Move(dx, dy);
        }
        
        // End turn after wandering.
        character.EndTurn();
    }
    
    /// <summary>
    /// Flee from danger.
    /// </summary>
    private static void Flee(Character character)
    {
        // TODO: Implement proper flee behavior.
        // For now, just wander away.
        Wander(character);
    }
}
