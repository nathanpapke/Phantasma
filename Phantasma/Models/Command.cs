using System;
using IronScheme.Runtime;

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
    
    // ===================================================================
    // HELPER METHODS - UI Interaction
    // ===================================================================
    
    /// <summary>
    /// Log a message to the console view.
    /// </summary>
    protected void Log(string message)
    {
        session.LogMessage(message);
    }
    
    /// <summary>
    /// Begin a group of log messages (will be collapsed in display).
    /// </summary>
    protected void LogBeginGroup()
    {
        // TODO: Implement log grouping when console supports it.
    }
    
    /// <summary>
    /// End a group of log messages.
    /// </summary>
    protected void LogEndGroup()
    {
        // TODO: Implement log grouping when console supports it.
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
    
    // ===================================================================
    // HELPER METHODS - Input Requests (Event-Driven)
    // ===================================================================
    
    /// <summary>
    /// Request a direction from the user.
    /// Pushes a DirectionKeyHandler and calls the callback when done.
    /// 
    /// This is the event-driven replacement for the blocking PromptForDirection().
    /// </summary>
    /// <param name="onDirection">
    /// Called when user selects direction or cancels.
    /// Receives Direction value, or null if cancelled.
    /// </param>
    protected void RequestDirection(Action<Direction?> onDirection)
    {
        var handler = new DirectionKeyHandler(onDirection);
        session.PushKeyHandler(handler);
    }
    
    /// <summary>
    /// Request a yes/no confirmation from the user.
    /// Pushes a YesNoKeyHandler and calls the callback when done.
    /// </summary>
    /// <param name="onResult">
    /// Called when user responds.
    /// Receives true for Yes, false for No/Cancel.
    /// </param>
    protected void RequestYesNo(Action<bool> onResult)
    {
        var handler = new YesNoKeyHandler(onResult);
        session.PushKeyHandler(handler);
    }
    
    // ===================================================================
    // HELPER METHODS - Conversion and Utility
    // ===================================================================
    
    /// <summary>
    /// Convert direction enum to display string.
    /// </summary>
    protected static string DirectionToString(Direction dir) => dir switch
    {
        Direction.North => "North",
        Direction.South => "South",
        Direction.East => "East",
        Direction.West => "West",
        Direction.NorthEast => "NE",
        Direction.NorthWest => "NW",
        Direction.SouthEast => "SE",
        Direction.SouthWest => "SW",
        Direction.Here => "Here",
        Direction.Up => "Up",
        Direction.Down => "Down",
        _ => "none"
    };
    
    /// <summary>
    /// Calculate Chebyshev distance (max of |dx|, |dy|).
    /// Used for range checking.
    /// </summary>
    protected static int CalculateDistance(int x1, int y1, int x2, int y2)
    {
        return Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1));
    }
    
    // ===================================================================
    // OPEN COMMAND - Event-Driven Implementation
    // ===================================================================
    
    /// <summary>
    /// Open Command - open doors, chests, containers.
    /// Mirrors Nazghul's cmdOpen().
    /// 
    /// Flow:
    /// 1. Show "Open-" prompt
    /// 2. Select party member (simplified: uses first member)
    /// 3. Show "&lt;direction&gt;" and wait for direction input
    /// 4. On direction received, find mechanism/container and open it
    /// </summary>
    /// <param name="pc">Character opening, or null to auto-select</param>
    public void Open(Character? pc = null)
    {
        ShowPrompt("Open-");
        
        // Get the party member who will open.
        if (pc == null)
        {
            pc = SelectPartyMember();
            if (pc == null)
            {
                ShowPrompt("none!");
                return;
            }
        }
        
        ShowPrompt($"Open-{pc.GetName()}-<direction>");
        
        // Capture pc in closure for callback.
        var actor = pc;
        
        // Request direction - this pushes a key handler
        RequestDirection(dir => CompleteOpen(actor, dir));
    }
    
    /// <summary>
    /// Complete the Open command after direction is received.
    /// Called by DirectionKeyHandler callback.
    /// </summary>
    private void CompleteOpen(Character pc, Direction? dir)
    {
        // Handle cancellation.
        if (dir == null)
        {
            ShowPrompt($"Open-{pc.GetName()}-none!");
            return;
        }
        
        // Update prompt with selected direction.
        ShowPrompt($"Open-{pc.GetName()}-{DirectionToString(dir.Value)}");
        
        // Calculate target coordinates.
        int dx = Common.DirectionToDx(dir.Value);
        int dy = Common.DirectionToDy(dir.Value);
        
        var place = pc.GetPlace();
        if (place == null)
        {
            Log("Open - no place!");
            return;
        }
        
        int x = place.WrapX(pc.GetX() + dx);
        int y = place.WrapY(pc.GetY() + dy);
        
        // Check for a mechanism (door, chest mechanism, etc.).
        var mech = place.GetObjectAt(x, y, ObjectLayer.Mechanism);
        
        // Check for a container.
        var container = place.GetObjectAt(x, y, ObjectLayer.Container);
        
        // Nothing to open?
        if (mech == null && container == null)
        {
            Log("Open - nothing there!");
            return;
        }
        
        // TODO: If both mech AND container present, prompt user to select.
        // For now, prioritize mechanism (doors over chests on same tile).
        
        // Open a mechanism (door, portcullis, etc.).
        if (mech != null)
        {
            var gifc = mech.Type?.InteractionHandler;
            if (gifc is Callable callable)
            {
                ShowPrompt($"Open-{pc.GetName()}-{mech.Name}!");
                Log($"{mech.Name}!");
                
                Console.WriteLine($"[Open] Sending 'open signal to {mech.Name}");
                
                try
                {
                    callable.Call("open", mech, pc);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Open] Error calling open handler: {ex.Message}");
                    Log($"Error: {ex.Message}");
                }
                
                return;
            }
            else
            {
                Log($"{mech.Name} can't be opened!");
                return;
            }
        }
        
        // Open a container (chest, barrel, corpse, etc.).
        if (container != null)
        {
            OpenContainer(pc, container);
        }
    }
    
    /// <summary>
    /// Open a container - check traps, spill contents, remove container.
    /// </summary>
    private void OpenContainer(Character pc, Object container)
    {
        ShowPrompt($"Open-{pc.GetName()}-{container.Name}!");
        
        LogBeginGroup();
        
        // Deduct action points.
        // TODO: pc.DecActionPoints(Common.NAZGHUL_BASE_ACTION_POINTS);
        
        /* to be uncommented when traps are implemented
        // Check for traps.
        if (container is Container cont && cont.IsTrapped)
        {
            var trap = cont.Trap;
            
            // Roll to disarm based on dexterity.
            var rand = new Random();
            if (rand.Next(999) < pc.GetDexterity())
            {
                Log("You disarm a trap!");
            }
            else
            {
                // Trigger the trap.
                Log("A trap is triggered!");
                if (trap is Callable trapCallable)
                {
                    try
                    {
                        trapCallable.Call(pc, container);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Open] Trap error: {ex.Message}");
                    }
                }
            }
        }
        */
        // Describe contents.
        Log("You find:");
        
        if (container is Container containerObj)
        {
            var contents = containerObj.GetContents();
            if (contents != null && contents.Count > 0)
            {
                foreach (var item in contents)
                {
                    string desc = item.Quantity > 1 
                        ? $"...{item.Name} x{item.Quantity}" 
                        : $"...{item.Name}";
                    Log(desc);
                }
            }
            else
            {
                Log("...nothing");
            }
            
            // Spill contents onto the map (open the container).
            containerObj.Open();
        }
        
        // Remove the container from the map.
        // TODO: Consider leaving empty containers.
        container.Remove();
        
        LogEndGroup();
    }
    
    // ===================================================================
    // TALK COMMAND
    // ===================================================================
    
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
            ShowPrompt("Talk-nobody there!");
            Log("Nobody nearby!");
            return;
        }
        
        // Start conversation with the NPC.
        ShowPrompt($"Talk-{nearestNPC.GetName()}");
        session.StartConversation(npcX, npcY);
    }
}
