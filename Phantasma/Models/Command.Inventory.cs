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
    public void Get(bool scoopAll = true)
    {
        if (session.Party == null || session.Player == null)
            return;
        
        ShowPrompt("Get-<direction>");
        
        // Request direction - callback will complete the command
        RequestDirection(dir => CompleteGet(dir, scoopAll));
    }
    
    /// <summary>
    /// Complete the Get command after direction is received.
    /// </summary>
    private void CompleteGet(Direction? dir, bool scoopAll)
    {
        if (dir == null)
        {
            ShowPrompt("Get-none!");
            return;
        }
        
        ShowPrompt($"Get-{DirectionToString(dir.Value)}");
        
        var player = session.Player;
        var place = player?.GetPlace();
        if (place == null)
        {
            return;
        }
        
        int dx = Common.DirectionToDx(dir.Value);
        int dy = Common.DirectionToDy(dir.Value);
        int targetX = player.GetX() + dx;
        int targetY = player.GetY() + dy;
        
        // Find first gettable item at location
        var item = place.GetFilteredObject(targetX, targetY, obj => obj.IsGettable());
        
        if (item == null)
        {
            Log("Get - nothing there!");
            return;
        }
        
        LogBeginGroup();
        
        // Get the first item
        GetItem(item);
        
        // If scooping all, get remaining items
        if (scoopAll)
        {
            while ((item = place.GetFilteredObject(targetX, targetY, 
                       obj => obj.IsGettable())) != null)
            {
                GetItem(item);
            }
        }
        
        LogEndGroup();
        
        // Deduct action points
        // TODO: player.DecActionPoints(Common.NAZGHUL_BASE_ACTION_POINTS);
    }
    
    /// <summary>
    /// Actually get a single item - transfer to party inventory.
    /// Helper for Get() command.
    /// </summary>
    private void GetItem(Object item)
    {
        if (session.Party == null)
            return;
        
        // Check if type has a custom 'get' handler via InteractionHandler (gifc).
        var gifc = item.Type?.InteractionHandler;
        if (gifc is IronScheme.Runtime.Callable callable && item.Type?.CanGet == true)
        {
            try
            {
                // Call the gifc with 'get signal: (gifc 'get item getter).
                callable.Call("get", item, session.Player);
                
                // The handler is responsible for removing the item and adding to inventory.
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Get] Handler error: {ex.Message}");
                // Fall through to default handling.
            }
        }
        
        // Default handling for items without custom handler.
        if (item is Item itemObj)
        {
            session.Party.Inventory.AddItem(itemObj);
            
            // Log what was picked up.
            string description = itemObj.Quantity > 1
                ? $"{itemObj.Name} x{itemObj.Quantity}"
                : itemObj.Name;
            Log($"You get: {description}");
            
            // Remove from map.
            item.Remove();
        }
        else
        {
            // Non-Item gettable object - just log and remove
            Log($"You get: {item.Name}");
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
    public void Ready(Character? member = null)
    {
        ShowPrompt("Ready-");
        
        // Select party member if not provided.
        if (member == null)
        {
            member = SelectPartyMember();
            if (member == null)
            {
                return;
            }
            
            // Check if charmed (charmed characters can't ready arms).
            if (member.IsCharmed)
            {
                Log("Charmed characters can't ready arms!");
                return;
            }
        }
        
        ShowPrompt($"Ready-{member.GetName()}-");
        
        LogBeginGroup();
        Log($"{member.GetName()} readies arms:");
        
        // TODO: Implement full ready UI with scroller
        // - Show Ready status mode with arms inventory
        // - Allow selecting/unselecting items
        // - Handle readying/unreadying based on selection
        
        Log("Ready command not fully implemented yet");
        LogEndGroup();
    }
    
    /// <summary>
    /// Unready Command - remove equipped items.
    /// Usually handled through Ready() in Nazghul, but can be separate.
    /// </summary>
    public void Unready(Character? member = null)
    {
        // TODO: Implement for later task
        Log("Unready command not yet implemented");
    }
    
    // ===================================================================
    // USE COMMAND - Use consumable items
    // ===================================================================
    
    /// <summary>
    /// Use Command - use items from inventory (potions, food, tools).
    /// </summary>
    /// <param name="member">Party member using the item, or null to prompt</param>
    public void Use(Character? member = null)
    {
        ShowPrompt("Use-");
        
        // Select party member if not provided.
        if (member == null)
        {
            member = SelectPartyMember();
            if (member == null)
            {
                ShowPrompt("Use-none!");
                return;
            }
        }
        
        ShowPrompt($"Use-{member.GetName()}-");
        
        // TODO: Implement full use UI
        // - Show Use status mode with usable items
        // - Allow selecting item
        // - Execute item's use effect (call Scheme closure)
        
        Log("Use command not fully implemented yet");
    }
    
    // ===================================================================
    // HANDLE COMMAND - Operate mechanisms
    // ===================================================================
    
    /// <summary>
    /// Handle Command - operate mechanisms (levers, buttons, switches).
    /// 
    /// Flow:
    /// 1. Show "Handle-" prompt
    /// 2. Select party member
    /// 3. Wait for direction input
    /// 4. Call mechanism's handle handler
    /// </summary>
    /// <param name="pc">Character handling, or null to prompt</param>
    public void Handle(Character? pc = null)
    {
        ShowPrompt("Handle-");
        
        // Get the party member.
        if (pc == null)
        {
            pc = SelectPartyMember();
            if (pc == null)
            {
                ShowPrompt("Handle-none!");
                return;
            }
        }
        
        ShowPrompt($"Handle-{pc.GetName()}-<direction>");
        
        // Capture pc in closure.
        var actor = pc;
        
        // Request direction.
        RequestDirection(dir => CompleteHandle(actor, dir));
    }
    
    /// <summary>
    /// Complete the Handle command after direction is received.
    /// </summary>
    private void CompleteHandle(Character pc, Direction? dir)
    {
        if (dir == null)
        {
            ShowPrompt($"Handle-{pc.GetName()}-none!");
            return;
        }
        
        ShowPrompt($"Handle-{pc.GetName()}-{DirectionToString(dir.Value)}");

        int dx = Common.DirectionToDx(dir.Value);
        int dy = Common.DirectionToDy(dir.Value);
        
        var place = pc.GetPlace();
        if (place == null)
            return ;
        
        int x = place.WrapX(pc.GetX() + dx);
        int y = place.WrapY(pc.GetY() + dy);
        
        // Check for a mechanism.
        var mech = place.GetObjectAt(x, y, ObjectLayer.Mechanism);
        
        if (mech == null)
        {
            Log("Handle - nothing there!");
            return;
        }
        
        if (!mech.CanHandle())
        {
            Log($"{mech.Name} can't be handled!");
            return;
        }
        
        // Call mechanism's handle handler.
        var gifc = mech.Type?.InteractionHandler;
        if (gifc is IronScheme.Runtime.Callable callable)
        {
            ShowPrompt($"Handle-{pc.GetName()}-{mech.Name}!");
            Log($"{mech.Name}!");
            
            Console.WriteLine($"[Handle] Sending 'handle signal to {mech.Name}");
            
            try
            {
                callable.Call("handle", mech, pc);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Handle] Error: {ex.Message}");
                Log($"Error: {ex.Message}");
            }
        }
        else
        {
            Log($"{mech.Name} has no handle action!");
        }
    }
    
    // ===================================================================
    // INVENTORY DISPLAY COMMAND
    // ===================================================================
    
    /// <summary>
    /// Inventory Command - show inventory UI.
    /// This is more of a display command than an action command.
    /// </summary>
    public void Inventory()
    {
        // TODO: Implement with Task 14 (Status Display)
        // Should show full inventory UI with status mode.
        
        // For now, just print inventory to console.
        if (session.Party?.Inventory == null)
        {
            Log("No inventory!");
            return;
        }
        
        Log("=== INVENTORY ===");
        
        var contents = session.Party.Inventory.GetContents();
        if (contents == null || contents.Count == 0)
        {
            Log("(empty)");
            return;
        }
        
        foreach (var item in contents)
        {
            string desc = item.Quantity > 1
                ? $"  {item.Name} x{item.Quantity}"
                : $"  {item.Name}";
            Log(desc);
        }
    }
}
