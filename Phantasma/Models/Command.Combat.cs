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
    /// </summary>
    /// <param name="direction">Direction to attack (optional for simplified version)</param>
    /// <returns>True if attack succeeded</returns>
    public void Attack(Direction? direction = null)
    {
        if (session.Player == null || session.Player.ActionPoints <= 0)
            return;
        
        var player = session.Player;
        var place = player.GetPlace();
        
        if (place == null)
        {
            Log("Player not on map!");
            return;
        }
        
        Log("Attack-");
        
        // Get weapon.
        var weapon = player.EnumerateArms();
        if (weapon == null)
        {
            Log("no weapon readied!");
            weapon = ArmsType.TestWeapons.Fists;
            Log("attacking with fists-");
        }
        
        if (!player.HasAmmo(weapon))
        {
            Log("no ammo!");
            return;
        }
        
        // Two paths: direct attack or targeted attack.
        if (direction.HasValue)
        {
            // Direct attack in direction.
            int dx = Common.DirectionToDx(direction.Value);
            int dy = Common.DirectionToDy(direction.Value);
            int targetX = player.GetX() + dx;
            int targetY = player.GetY() + dy;
            
            Log($"{DirectionToString(direction.Value)}-");
            ExecuteAttack(targetX, targetY, weapon);
        }
        else
        {
            // Begin targeting - use callback for completion.
            BeginTargetSelection(weapon, (targetX, targetY, cancelled) =>
            {
                if (cancelled)
                {
                    Log("none!");
                    return;
                }
                
                ExecuteAttack(targetX, targetY, weapon);
            });
        }
    }
    
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
    public bool Fire()
    {
        var party = session.Party;
    
        if (party?.Vehicle == null || party.Vehicle.GetOrdnance() == null)
        {
            Log("Fire-No cannons available!");
            return false;
        }
    
        var vehicle = party.Vehicle;
        var ordnance = vehicle.GetOrdnance();
    
        Log($"Fire {ordnance.Name}-");
        ShowPrompt("Fire-<direction>");
    
        var dir = PromptForDirection();
        if (dir == Direction.None)
        {
            Log("None!");
            return false;
        }
    
        int dx = Common.DirectionToDx(dir);
        int dy = Common.DirectionToDy(dir);
    
        Log($"{DirectionToString(dir)}-");
    
        if (!vehicle.FireWeapon(dx, dy, party))
        {
            Log(vehicle.GetFiringRestrictionMessage());
            return false;
        }
    
        Log("Hits away!");
        return true;
    }
}
