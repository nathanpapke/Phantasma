using System;
using System.Collections.Generic;
using System.Linq;
using IronScheme;
using IronScheme.Runtime;
using IronScheme.Scripting;

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
                object toCall = character.AIBehavior;
                
                // Resolve symbol to closure if needed (deferred resolution).
                // This handles quoted symbols like 'spider-ai that weren't resolved at creation.
                if (toCall is SymbolId symbolId)
                {
                    var symbolName = SymbolTable.IdToString(symbolId);
                    Console.WriteLine($"[Behavior.Execute] Resolving AI symbol '{symbolName}'...");
                    toCall = symbolName.Eval();
                    
                    // Cache the resolved closure for future calls.
                    character.AIBehavior = toCall;
                }
                
                if (toCall is Callable callable)
                {
                    Console.WriteLine($"[Behavior.Execute] {character.GetName()} calling custom AI closure...");
                    callable.Call(character);
                }
                else
                {
                    Console.WriteLine($"[Behavior.Execute] {character.GetName()}'s AI is not callable: {toCall?.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                // Try to get more details from SchemeException.
                var message = ex.Message;
                if (ex.InnerException != null)
                    message += $" Inner: {ex.InnerException.Message}";
                
                // IronScheme exceptions often have useful info in Data or ToString.
                Console.WriteLine($"[NpcAI] Custom AI error for {character.GetName()}: {message}");
                Console.WriteLine($"[NpcAI] Full exception: {ex}");
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
        Console.WriteLine($"[Idle] {character.GetName()} - Target: {target?.GetName() ?? "none"}");
        
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
        {
            Console.WriteLine($"[SelectTarget] {attacker.GetName()}: No place!");
            return null;
        }
        
        Console.WriteLine($"[SelectTarget] {attacker.GetName()} scanning {place.GetAllBeings().Count} beings...");
        
        Character? bestTarget = null;
        int bestDistance = int.MaxValue;
        
        // Search all beings in the place.
        foreach (var obj in place.GetAllBeings())
        {
            if (obj == attacker) continue;
            
            // Add detailed logging.
            if (obj is Character target)
            {
                bool isHostile = AreHostile(attacker, target);
                bool canSee = attacker.CanSee(target);
                bool isDead = target.IsDead;
                bool isOnMap = target.IsOnMap();
                
                Console.WriteLine($"  Checking {target.GetName()}: hostile={isHostile}, canSee={canSee}, dead={isDead}, onMap={isOnMap}");
                
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
            Console.WriteLine($"[AreHostile] {a.GetName()}(f{factionA}) vs {b.GetName()}(f{factionB}): relation={relation}");
            return relation < 0;  // Negative = hostile
        }
        
        // Different factions without diplomacy table = hostile by default
        // This matches Nazghul's behavior where different factions are enemies
        // unless the diplomacy table explicitly says otherwise.
        Console.WriteLine($"[AreHostile] No diplomacy table! Fallback check.");
        return true;
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
        
        // Play weapon fire sound.
        if (weapon.FireSound != null)
            SoundManager.Instance.Play(weapon.FireSound);
        
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
        
        // Play damage sound.
        if (target.Species.DamageSound != null)
            SoundManager.Instance.Play(target.Species.DamageSound);
        
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
            character.DecreaseActionPoints(1);
            return;
        }
        
        // Roll for direction.  Ensure at least one is non-zero.
        int dx = random.Next(3) - 1;
        int dy = random.Next(3) - 1;
        
        // If both are zero, pick a random non-zero direction.
        if (dx == 0 && dy == 0)
        {
            if (random.Next(2) == 0)
                dx = random.Next(2) == 0 ? -1 : 1;
            else
                dy = random.Next(2) == 0 ? -1 : 1;
        }
        
        int newX = character.GetX() + dx;
        int newY = character.GetY() + dy;
        
        // Don't wander off map.
        if (place.IsOffMap(newX, newY))
        {
            character.DecreaseActionPoints(1);
            return;
        }
        
        // Don't wander into hazards.
        if (place.IsHazardous(newX, newY))
        {
            character.DecreaseActionPoints(1);
            return;
        }
        
        // Try to move (Move() consumes AP on success).
        if (!character.Move(dx, dy))
        {
            // Consume 1 AP for the attempt.
            character.DecreaseActionPoints(1);
        }
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
