using System;

namespace Phantasma.Models;

/// <summary>
/// Game Command Implementations
/// </summary>
public class Command
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
    /// Get Command - pick up items from the ground.
    /// </summary>
    /// <param name="scoopAll">If true, get ALL items at location (default behavior)</param>
    public bool Get(bool scoopAll = true)
    {
        if (session.Party == null || session.Player == null)
            return false;
        
        Log("Get-");
        
        // Request direction from user.
        // For now, we'll implement a simple version that tries the facing direction.
        // Full implementation would use PromptForDirection() with UI.
        
        // Get player's current position.
        var player = session.Player;
        var place = player.GetPlace();
        if (place == null)
            return false;
        
        // For MVP: Try north of player.
        // TODO: Implement direction prompt UI.
        int dx = 0, dy = -1;

        int targetX = player.GetX() + dx;
        int targetY = player.GetY() + dy;
        
        var item = place.GetFilteredObject(targetX, targetY, obj => obj.IsGettable());
        
        if (item == null)
        {
            Log("Get - nothing there!");
            return false;
        }
        
        LogBeginGroup();
        
        // Get the first item.
        GetItem(item);
        
        if (scoopAll)
        {
            while ((item = place.GetFilteredObject(targetX, targetY, 
                obj => obj.IsGettable())) != null)
            {
                GetItem(item);
            }
        }
        
        LogEndGroup();
        
        // Nazghul: mapSetDirty();
        // session.SetMapDirty();
        
        // Nazghul: actor->decActionPoints(NAZGHUL_BASE_ACTION_POINTS);
        // player.DecActionPoints(10);
        
        return true;
    }
    
    /// <summary>
    /// Actually get a single item - transfer to party inventory.
    /// </summary>
    private void GetItem(Object item)
    {
        if (session.Party == null)
            return;
        
        // Add to party inventory.
        if (item is Item itemObj)
        {
            session.Party.Inventory.AddItem(itemObj);
            
            // Log what was picked up.
            string description = itemObj.Name;
            if (itemObj.Quantity > 1)
            {
                description = $"{itemObj.Name} x{itemObj.Quantity}";
            }
            Log($"You get: {description}");
            
            // Remove from map.
            item.Remove();
        }
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
    /// Drop Command - drop items from inventory.
    /// </summary>
    public bool Drop()
    {
        // TODO: Implement later
        Log("Drop command not yet implemented");
        return false;
    }
    
    /// <summary>
    /// Inventory Command - show inventory UI.
    /// </summary>
    public bool Inventory()
    {
        // TODO: Implement with Task 14 (Status Display)
        Log("Inventory command not yet implemented");
        
        // For now, just print inventory to console.
        if (session.Party?.Inventory != null)
        {
            var items = session.Party.Inventory.GetContents();
            Log($"Inventory ({items.Count} types):");
            foreach (var item in items)
            {
                Log($"  - {item.Name} x{item.Quantity}");
            }
        }
        return true;
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
}

/// <summary>
/// Direction enumeration for movement and targeting.
/// Matches Nazghul's direction system.
/// </summary>
public enum Direction
{
    None,
    North,
    South,
    East,
    West,
    NorthEast,
    NorthWest,
    SouthEast,
    SouthWest
}
