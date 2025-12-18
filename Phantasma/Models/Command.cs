using System;

namespace Phantasma.Models;

/// <summary>
/// Game Command Implementations
/// </summary>
public partial class Command
{
    private readonly Session session;
    
    // Command State for Multi-step Commands
    private bool awaitingDirection = false;
    private Action<Direction>? directionCallback = null;
    
    public Command(Session session)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
    }
    
    /// <summary>
    /// Open Command - open containers/doors.
    /// </summary>
    public bool Open()
    {
        // TODO: Implement for Task 9 completion
        Log("Open command not yet implemented");
        return false;
    }
    
    /// <summary>
    /// Start a conversation with an NPC.
    /// </summary>
    public void Talk()
    {
        if (session.Player == null || session.CurrentPlace == null)
        {
            Log("Talk - no player or place");
            return;
        }
        
        Log("Talk-");
        
        // Get player position.
        int playerX = session.Player.GetX();
        int playerY = session.Player.GetY();
        
        // For MVP: Check adjacent tiles for NPCs.
        // Full implementation would use target selection UI.
        
        // Check all 8 adjacent directions.
        int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
        int[] dy = { -1, -1, 0, 1, 1, 1, 0, -1 };
        
        Character nearestNPC = null;
        int npcX = 0, npcY = 0;
        
        for (int i = 0; i < 8; i++)
        {
            int checkX = playerX + dx[i];
            int checkY = playerY + dy[i];
            
            var being = session.CurrentPlace.GetBeingAt(checkX, checkY);
            if (being is Character character && !character.IsPlayer)
            {
                nearestNPC = character;
                npcX = checkX;
                npcY = checkY;
                break;
            }
        }
        
        if (nearestNPC == null)
        {
            Log("Nobody nearby!");
            return;
        }
        
        // Start conversation with the NPC.
        session.StartConversation(npcX, npcY);
    }

    /// <summary>
    /// Start target selection mode.
    /// When complete, calls the callback with (x, y, cancelled).
    /// </summary>
    private void BeginTargetSelection(ArmsType weapon, Action<int, int, bool> onComplete)
    {
        var player = session.Player;
        
        // Determine starting position for cursor.
        var lastTarget = player.GetAttackTarget();
        int startX, startY;
        
        if (lastTarget != null && lastTarget.Position?.Place == player.GetPlace())
        {
            // Remember last target position.
            startX = lastTarget.GetX();
            startY = lastTarget.GetY();
        }
        else
        {
            // Default to player position
            startX = player.GetX();
            startY = player.GetY();
        }
        
        // Delegate to Session to manage the targeting UI
        session.BeginTargeting(
            player.GetX(), 
            player.GetY(), 
            weapon.Range,
            startX,
            startY,
            onComplete
        );
    }

    /// <summary>
    /// Execute an attack on a target at given coordinates.
    /// Handles both melee and ranged attacks with missile animation.
    /// </summary>
    private void ExecuteAttack(int targetX, int targetY, ArmsType weapon)
    {
        var player = session.Player;
        var place = player.GetPlace();
        
        if (place == null)
        {
            Log("Not on a map!");
            return;
        }
        
        // Get the target being.
        var target = place.GetBeingAt(targetX, targetY);
        
        if (target == null)
        {
            Log("nobody there!");
            return;
        }
        
        Log($"{target.GetName()}-");
        
        // Check if target is non-hostile (prompt for confirmation in full implementation).
        // For now, just proceed.
        
        // Log the attack.
        Console.WriteLine($"You attack {target.GetName()}.");
        
        // Check range.
        int distance = CalculateDistance(
            player.GetX(), player.GetY(),
            targetX, targetY);
        
        if (distance > weapon.Range)
        {
            Log("out of range!");
            return;
        }
        
        // Fire the weapon (this will animate missiles for ranged weapons).
        bool hit = weapon.Fire(target, player.GetX(), player.GetY());
        
        if (!hit)
        {
            Log("missed!");
            player.ActionPoints -= weapon.RequiredActionPoints;
            player.UseAmmo(weapon);
            return;
        }
        
        // Weapon fired successfully, now roll for hit.
        int toHit = Dice.Roll("1d20") + Dice.Roll(weapon.ToHitDice);
        int defense = target.GetDefend();
        
        if (toHit < defense)
        {
            Log("barely scratched!");
            player.ActionPoints -= weapon.RequiredActionPoints;
            player.UseAmmo(weapon);
            return;
        }
        
        // Hit! Roll for damage.
        int damage = Dice.Roll(weapon.DamageDice);
        int armor = target.GetArmor();
        damage = Math.Max(0, damage - armor);
        
        target.Damage(damage);
        
        Log($"{target.GetWoundDescription()}!");
        
        // Spend action points and ammo.
        player.ActionPoints -= weapon.RequiredActionPoints;
        player.UseAmmo(weapon);
        
        // Award experience if target was killed.
        if (target.IsDead)
        {
            player.AddExperience(target.GetExperienceValue());
        }
    }
    
    /// <summary>
    /// Select a target using the crosshair cursor.
    /// </summary>
    private void SelectTarget(int originX, int originY, int range, 
                             int startX, int startY,
                             Action<int, int, bool> onComplete)
    {
        session.BeginTargeting(originX, originY, range, startX, startY, onComplete);
    }

    /// <summary>
    /// Attack with a specific weapon and direction.
    /// Useful for testing and AI.
    /// </summary>
    public bool AttackWith(ArmsType weapon, Direction direction)
    {
        if (session.Player == null || weapon == null)
            return false;
    
        var player = session.Player;
        var place = player.GetPlace();
    
        if (place == null)
            return false;
    
        // Get target.
        int dx = Common.DirectionToDx(direction);
        int dy = Common.DirectionToDy(direction);
        int targetX = player.GetX() + dx;
        int targetY = player.GetY() + dy;
    
        var target = place.GetBeingAt(targetX, targetY);
        if (target == null || target == player)
            return false;
    
        // Attack directly.
        return player.Attack(weapon, target as Character);
    }

    /// <summary>
    /// Auto-attack: find nearest enemy and attack.
    /// Useful for quick testing.
    /// </summary>
    public bool AutoAttack()
    {
        if (session.Player == null)
            return false;
    
        var player = session.Player;
        var place = player.GetPlace();
    
        if (place == null)
            return false;
    
        // Find nearest being (simplified - just check adjacent tiles).
        var directions = new[]
        {
            Direction.North, Direction.South, Direction.East, Direction.West,
            Direction.NorthEast, Direction.NorthWest, Direction.SouthEast, Direction.SouthWest
        };
    
        foreach (var dir in directions)
        {
            int dx = Common.DirectionToDx(dir);
            int dy = Common.DirectionToDy(dir);
            int targetX = player.GetX() + dx;
            int targetY = player.GetY() + dy;
        
            var target = place.GetBeingAt(targetX, targetY);
            if (target != null && target != player)
            {
                // Found a target!
                Attack(dir);
                return true;
            }
        }
    
        Log("No targets in range!");
        return false;
    }

    /// <summary>
    /// Calculate Chebyshev distance (max of dx, dy).
    /// </summary>
    private int CalculateDistance(int x1, int y1, int x2, int y2)
    {
        return Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1));
    }

    /// <summary>
    /// Convert direction enum to string.
    /// </summary>
    private string DirectionToString(Direction dir) => dir switch
    {
        Direction.North => "North",
        Direction.South => "South",
        Direction.East => "East",
        Direction.West => "West",
        Direction.NorthEast => "NE",
        Direction.NorthWest => "NW",
        Direction.SouthEast => "SE",
        Direction.SouthWest => "SW",
        _ => "None"
    };
    
    /// <summary>
    /// Prompt user for direction.
    /// This would show "Get-&lt;direction&gt;" and wait for arrow key.
    /// </summary>
    private Direction PromptForDirection()
    {
        // TODO: Implement UI prompt
        // For now, return None - caller should handle this
        // 
        // Full implementation:
        // 1. Set awaitingDirection = true
        // 2. Store callback for when direction received
        // 3. MainWindow.OnKeyDown detects direction keys
        // 4. Calls ProcessDirection()
        // 5. Triggers callback
        
        return Direction.None;
    }
    
    /// <summary>
    /// Prompt for a yes/no response.
    /// Mirrors Nazghul's ui_get_yes_no().
    /// </summary>
    /// <param name="prompt">The question to ask (e.g., "Quit & Save Game")</param>
    /// <returns>True for yes, false for no</returns>
    protected bool PromptForYesNo(string prompt)
    {
        // Show the prompt with Y/N indicator
        ShowPrompt($"{prompt}-<Y/N>");
    
        // TODO: Full implementation should:
        // 1. Push a YesNoKeyHandler onto the key handler stack
        // 2. Wait for user to press Y, N, or Escape
        // 3. Pop the handler when done
        // 4. Return the result
    
        // SIMPLIFIED VERSION for now:
        // Default to Yes until key handlers are implemented
        bool result = true;
    
        // Clear and show result
        ClearPrompt();
        ShowPrompt(result ? "Yes!" : "No");
    
        return result;
    }
    
    /// <summary>
    /// Process direction input (called by MainWindow when awaiting direction).
    /// </summary>
    public void ProcessDirection(Direction direction)
    {
        if (awaitingDirection && directionCallback != null)
        {
            awaitingDirection = false;
            var callback = directionCallback;
            directionCallback = null;
            callback(direction);
        }
    }
    /*
    /// <summary>
    /// Convert direction to X offset.
    /// Equivalent to Nazghul's directionToDx().
    /// </summary>
    public static int DirectionToDx(Direction dir) => dir switch
    {
        Direction.East => 1,
        Direction.West => -1,
        Direction.NorthEast => 1,
        Direction.NorthWest => -1,
        Direction.SouthEast => 1,
        Direction.SouthWest => -1,
        _ => 0
    };
    
    /// <summary>
    /// Convert direction to Y offset.
    /// Equivalent to Nazghul's directionToDy().
    /// </summary>
    public static int DirectionToDy(Direction dir) => dir switch
    {
        Direction.North => -1,
        Direction.South => 1,
        Direction.NorthEast => -1,
        Direction.NorthWest => -1,
        Direction.SouthEast => 1,
        Direction.SouthWest => 1,
        _ => 0
    };
    */
    // Logging helpers - connect to your actual log system
    private void Log(string message)
    {
        // TODO: Connect to game log window
        Console.WriteLine(message);
    }
    
    private void LogBeginGroup()
    {
        // Nazghul's log_begin_group() for collapsing multiple messages
    }
    
    private void LogEndGroup()
    {
        // Nazghul's log_end_group()
    }
    
    /// <summary>
    /// Show a command prompt (e.g., "Talk-", "Ready-").
    /// </summary>
    protected void ShowPrompt(string prompt)
    {
        session.SetCommandPrompt(prompt);
    }
    
    /// <summary>
    /// Clear the command prompt.
    /// </summary>
    protected void ClearPrompt()
    {
        session.SetCommandPrompt("");
    }
}
