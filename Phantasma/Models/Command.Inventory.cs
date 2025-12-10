using System;

namespace Phantasma.Models;

/// <summary>
/// Command.Inventory - Inventory Management Commands
/// 
/// Commands for managing items and equipment:
/// - Get: Pick up items from the ground
/// - Drop: Drop items from inventory
/// - Ready: Equip weapons/armor
/// - Unready: Remove equipped items
/// - Use: Use consumable items
/// - Open: Open containers and doors
/// - Handle: Operate mechanisms (levers, buttons, etc)
/// </summary>
public partial class Command
{
    // ===================================================================
    // GET COMMAND - Pick up items
    // ===================================================================
    
    /// <summary>
    /// Get Command - pick up items from the ground.
    /// </summary>
    /// <param name="scoopAll">If true, get ALL items at location (default behavior)</param>
    public bool Get(bool scoopAll = true)
    {
        if (session.Party == null || session.Player == null)
            return false;
        
        ShowPrompt("Get-");
        
        // Request direction from user.
        var dir = PromptForDirection();
        if (dir == null)
        {
            ClearPrompt();
            return false;
        }
        
        // Get player's current position.
        var player = session.Player;
        var place = player.GetPlace();
        if (place == null)
            return false;

        int dx = DirectionToDx(dir);
        int dy = DirectionToDy(dir);
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
        
        // TODO: mapSetDirty() when map refresh is implemented
        // TODO: player.DecActionPoints() when action points are implemented
        
        return true;
    }
    
    /// <summary>
    /// Actually get a single item - transfer to party inventory.
    /// Helper for Get() command.
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
    
    // ===================================================================
    // DROP COMMAND - Drop items from inventory
    // ===================================================================
    
    /// <summary>
    /// Drop Command - drop items from inventory onto the ground.
    /// </summary>
    public bool Drop()
    {
        // TODO: Implement for later task
        // - Select item from inventory
        // - Prompt for quantity
        // - Place on ground at player location
        Log("Drop command not yet implemented");
        return false;
    }
    
    // ===================================================================
    // READY/UNREADY COMMANDS - Equip/unequip weapons and armor
    // ===================================================================
    
    /// <summary>
    /// Ready Command - equip weapons or armor.
    /// Mirrors Nazghul's cmdReady().
    /// </summary>
    /// <param name="member">Party member to ready for, or null to prompt</param>
    public bool Ready(Character? member = null)
    {
        ShowPrompt("Ready-");
        
        // Select party member if not provided.
        if (member == null)
        {
            member = SelectPartyMember();
            if (member == null)
            {
                return false;
            }
            
            // Check if charmed (charmed characters can't ready arms).
            if (member.IsCharmed)
            {
                Log("Charmed characters can't ready arms!");
                return false;
            }
            
            ShowPrompt("-");
        }
        
        LogBeginGroup();
        Log($"{member.GetName()} readies arms:");
        
        // TODO: Implement full ready UI with scroller
        // - Show Ready status mode with arms inventory
        // - Allow selecting/unselecting items
        // - Handle readying/unreadying based on selection
        
        Log("Ready command not fully implemented yet");
        LogEndGroup();
        
        return false;
    }
    
    /// <summary>
    /// Unready Command - remove equipped items.
    /// Usually handled through Ready() in Nazghul, but can be separate.
    /// </summary>
    public bool Unready(Character? member = null)
    {
        // TODO: Implement for later task
        Log("Unready command not yet implemented");
        return false;
    }
    
    // ===================================================================
    // USE COMMAND - Use consumable items
    // ===================================================================
    
    /// <summary>
    /// Use Command - use items from inventory (potions, food, tools).
    /// Mirrors Nazghul's cmdUse().
    /// </summary>
    /// <param name="member">Party member using the item, or null to prompt</param>
    public bool Use(Character? member = null)
    {
        ShowPrompt("Use-");
        
        // Select party member if not provided.
        if (member == null)
        {
            member = SelectPartyMember();
            if (member == null)
            {
                return false;
            }
            ShowPrompt("-");
        }
        
        // TODO: Implement full use UI
        // - Show Use status mode with usable items
        // - Allow selecting item
        // - Execute item's use effect (call Scheme closure)
        
        Log("Use command not fully implemented yet");
        
        return false;
    }
    
    // ===================================================================
    // OPEN COMMAND - Open containers and doors
    // ===================================================================
    
    /// <summary>
    /// Open Command - open containers/doors.
    /// Mirrors Nazghul's cmdOpen().
    /// </summary>
    /// <param name="pc">Character opening, or null to prompt</param>
    public bool Open(Character? pc = null)
    {
        ShowPrompt("Open-");
        
        // Get the party member who will open.
        if (pc == null)
        {
            pc = SelectPartyMember();
            if (pc == null)
            {
                return false;
            }
        }
        
        ShowPrompt(pc.GetName() + "-");
        
        // Get direction to open.
        var dir = PromptForDirection();
        if (dir == null)
        {
            return false;
        }
        
        int dx = DirectionToDx(dir);
        int dy = DirectionToDy(dir);
        
        int x = pc.GetX() + dx;
        int y = pc.GetY() + dy;
        
        var place = pc.GetPlace();
        if (place == null)
            return false;
        
        // Check for a mechanism (door, chest, etc).
        var mech = place.GetObjectAt(x, y, ObjectLayer.Mechanism);
        
        // Check for a container.
        var container = place.GetObjectAt(x, y, ObjectLayer.Container);
        
        if (mech == null && container == null)
        {
            Log("Open - nothing there to open!");
            return false;
        }
        
        // TODO: Implement full open logic.
        // - If both mech and container present, prompt to select.
        // - Call mech/container's open handler.
        // - Handle locked/unlocked states.
        
        Log("Open command not fully implemented yet");
        
        return false;
    }
    
    // ===================================================================
    // HANDLE COMMAND - Operate mechanisms
    // ===================================================================
    
    /// <summary>
    /// Handle Command - operate mechanisms (levers, buttons, switches).
    /// Mirrors Nazghul's cmdHandle().
    /// </summary>
    /// <param name="pc">Character handling, or null to prompt</param>
    public bool Handle(Character? pc = null)
    {
        ShowPrompt("Handle-");
        
        // Get the party member.
        if (pc == null)
        {
            pc = SelectPartyMember();
            if (pc == null)
            {
                return false;
            }
        }
        
        ShowPrompt(pc.GetName() + "-");
        
        // Get direction.
        var dir = PromptForDirection();
        if (dir == null)
        {
            return false;
        }

        int dx = DirectionToDx(dir);
        int dy = DirectionToDy(dir);
        
        int x = pc.GetX() + dx;
        int y = pc.GetY() + dy;
        
        var place = pc.GetPlace();
        if (place == null)
            return false;
        
        // Check for a mechanism.
        var mech = place.GetObjectAt(x, y, ObjectLayer.Mechanism);
        
        if (mech == null || !mech.CanHandle())
        {
            Log("Handle - nothing there to handle!");
            return false;
        }
        
        // TODO: Implement full handle logic.
        // - Call mechanism's handle closure.
        // - Handle results (door opening, trap triggering, etc).
        
        Log("Handle command not fully implemented yet");
        
        return false;
    }
    
    // ===================================================================
    // INVENTORY DISPLAY COMMAND
    // ===================================================================
    
    /// <summary>
    /// Inventory Command - show inventory UI.
    /// This is more of a display command than an action command.
    /// </summary>
    public bool Inventory()
    {
        // TODO: Implement with Task 14 (Status Display)
        // Should show full inventory UI with status mode.
        
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
}
