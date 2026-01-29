using System;

namespace Phantasma.Models;

/// <summary>
/// Command.Combat - Combat Commands
/// 
/// Commands for combat:
/// - Attack: Melee attack in a direction
/// - Fire: Fire vehicle weapons (cannons, etc)
/// </summary>
public partial class Command
{
    // ===================================================================
    // ATTACK COMMAND - Melee Combat
    // ===================================================================

    /// <summary>
    /// Attack Command - attack an enemy in a direction.
    /// 
    /// Flow for melee (range 1):
    /// 1. Show "Attack-" prompt
    /// 2. Wait for direction
    /// 3. Attack target in that direction
    /// 
    /// Flow for ranged (range > 1):
    /// 1. Show "Attack-" prompt  
    /// 2. Enter targeting mode
    /// 3. Player moves crosshair to target
    /// 4. Attack target at crosshair position
    /// </summary>
    /// <param name="direction">Direction to attack (optional for simplified version)</param>
    /// <returns>True if attack succeeded</returns>
    public void Attack(Direction? direction = null)
    {
        var player = session.Player;
        if (player == null) return;
        
        var weapon = player.GetCurrentWeapon();
        int range = weapon?.Range ?? 1;
        
        int playerX = player.GetX();
        int playerY = player.GetY();
        
        if (range <= 1)
        {
            // Melee attack - range 1, auto-confirm on direction.
            ShowPrompt("Attack-<target>");
            
            session.BeginTargeting(
                playerX, playerY, 1, playerX, playerY,
                (targetX, targetY, cancelled) => CompleteMeleeAttack(player, targetX, targetY, cancelled)
            );
        }
        else
        {
            // Ranged attack - requires Enter to confirm.
            ShowPrompt("Attack-<select target>");
        
            session.BeginTargeting(
                playerX, playerY, range, playerX, playerY,
                (targetX, targetY, cancelled) => CompleteRangedAttack(player, weapon, !cancelled, targetX, targetY)
            );
        }
    }
    
    /// <summary>
    /// Complete a melee attack after direction received.
    /// </summary>
    private void CompleteMeleeAttack(Character attacker, int targetX, int targetY, bool cancelled)
    {
        if (cancelled)
        {
            ShowPrompt("Attack-none!");
            return;
        }
        
        var place = attacker.GetPlace();
        if (place == null) return;
        
        var target = place.GetBeingAt(targetX, targetY);
        
        if (target == null)
        {
            Log("Attack - nobody there!");
            return;
        }
        
        // Check if target is already dead.
        if (target.IsDead)
        {
            Log("Attack - already dead!");
            return;
        }
        
        if (target == attacker)
        {
            Log("You can't attack yourself!");
            return;
        }
        
        // Execute the attack with current weapon.
        ExecuteAttack(attacker, target, attacker.GetCurrentWeapon());
    }
    
    /// <summary>
    /// Complete a ranged attack after targeting.
    /// Note: BeginTargeting automatically handles cleanup when callback is invoked.
    /// </summary>
    private void CompleteRangedAttack(Character attacker, ArmsType? weapon, 
        bool confirmed, int targetX, int targetY)
    {
        // Note: targeting cleanup (IsTargeting=false, PopKeyHandler) is done by Session.BeginTargeting.
        
        if (!confirmed)
        {
            ShowPrompt("Attack-cancelled!");
            return;
        }
        
        var place = attacker.GetPlace();
        if (place == null)
        {
            return;
        }
        
        // Find target at crosshair.
        var target = place.GetBeingAt(targetX, targetY);
        
        if (target == null)
        {
            Log("Attack - nobody there!");
            return;
        }
        
        // Remember this target for next time.
        attacker.SetAttackTarget(target);
        
        // Execute the attack.
        ExecuteAttack(attacker, target, weapon);
    }
    
    /// <summary>
    /// Execute an attack against a target.
    /// </summary>
    private void ExecuteAttack(Character attacker, Being target, ArmsType? weapon)
    {
        // Check if target is already dead.
        if (target.IsDead)
        {
            Log("already dead!");
            return;
        }
        
        // Use natural weapon if none provided.
        if (weapon == null)
        {
            weapon = attacker.Species.Weapon;
        }
        
        if (weapon == null)
        {
            Log("no weapon!");
            return;
        }
        
        string weaponName = weapon.Name ?? "fists";
        
        // Log attack start: "Attacker: Weapon - Target "
        Log($"{attacker.GetName()}: {weaponName} - {target.GetName()} ");
        
        // Fire the weapon (handles missile animation for ranged weapons).
        // Returns false if projectile missed (e.g., hit terrain).
        bool hit = weapon.Fire(target, attacker.GetX(), attacker.GetY());
        
        // Consume action points and ammo.
        attacker.DecreaseActionPoints(weapon.RequiredActionPoints);
        attacker.UseAmmo(weapon);
        
        if (!hit)
        {
            Log("missed!");
            return;
        }
        
        // Roll to hit: 1d20 + weapon's to-hit dice
        int toHitRoll = Dice.Roll("1d20") + Dice.Roll(weapon.ToHitDice);
        int defense = target.GetDefend();
        
        Console.WriteLine($"[Combat] To-hit: {toHitRoll} vs Defense: {defense}");
        
        if (toHitRoll < defense)
        {
            Log("barely scratched!");
            return;
        }
        
        // Roll for damage: weapon damage dice - target armor
        int damage = Dice.Roll(weapon.DamageDice);
        int armor = target.GetArmor();
        damage -= armor;
        damage = Math.Max(0, damage);
        
        Console.WriteLine($"[Combat] Damage: {damage} (rolled - {armor} armor)");
        
        // Apply damage.
        target.Damage(damage);
        
        Log($"{target.GetWoundDescription()}!");
        
        // Award XP if target was killed.
        if (target.IsDead)
        {
            int xp = target.GetExperienceValue();
            attacker.AddExperience(xp);
            Log($"{attacker.GetName()} gains {xp} XP!");
        }
        
        session.CheckAndProcessTurnEnd();
    }
    /*
    /// <summary>
    /// Calculate damage for an attack.
    /// </summary>
    private int CalculateDamage(Character attacker, ArmsType? weapon)
    {
        var rand = new Random();
        
        int baseDamage = weapon?.BaseDamage ?? 1;
        int bonusDamage = weapon?.BonusDamage ?? 0;
        int strengthBonus = attacker.GetStrength() / 10;
        
        // Roll damage dice.
        int damage = baseDamage + rand.Next(bonusDamage + 1) + strengthBonus;
        
        return Math.Max(1, damage);  // Minimum 1 damage
    }
    */
    // ===================================================================
    // FIRE COMMAND - Vehicle weapons
    // ===================================================================
    
    /// <summary>
    /// Fire Command - fire vehicle-mounted weapons.
    /// 
    /// Flow:
    /// 1. Check if vehicle has ordnance
    /// 2. Prompt for direction
    /// 3. Fire weapon if it's a valid broadside
    /// </summary>
    public void Fire()
    {
        var player = session.Player;
        if (player == null)
        {
            return;
        }
        
        // Check if player is in a vehicle.
        var vehicle = session.Party?.Vehicle;
        if (vehicle == null)
        {
            Log("Fire - not in a vehicle!");
            return;
        }
        
        // Check if vehicle has ordnance.
        var ordnance = vehicle.GetOrdnance();
        if (ordnance == null)
        {
            Log("Fire - vehicle has no weapons!");
            return;
        }
        
        ShowPrompt("Fire-<direction>");
        
        // Capture for closure
        var v = vehicle;
        var o = ordnance;
        
        RequestDirection(dir => CompleteFireVehicle(player, v, o, dir));
    }
    
    /// <summary>
    /// Complete the Fire command after direction received.
    /// </summary>
    private void CompleteFireVehicle(Character player, Vehicle vehicle, 
        ArmsType ordnance, Direction? dir)
    {
        if (dir == null)
        {
            ShowPrompt("Fire-none!");
            return;
        }
        
        ShowPrompt($"Fire-{DirectionToString(dir.Value)}");
        
        int dx = Common.DirectionToDx(dir.Value);
        int dy = Common.DirectionToDy(dir.Value);
        
        // Check if vehicle can fire in this direction.
        if (!vehicle.CanFireInDirection(dx, dy))
        {
            Log($"Fire - can't fire {DirectionToString(dir.Value)} from this vehicle!");
            return;
        }
        
        // Fire!
        Log($"Fire {ordnance.Name}!");
        vehicle.FireWeapon(dx, dy, player);
        
        // TODO: Animation, sound effects.
    }
}
