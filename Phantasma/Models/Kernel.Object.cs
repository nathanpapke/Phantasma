using System;
using IronScheme;
using IronScheme.Runtime;

namespace Phantasma.Models;

public partial class Kernel
{
    // ===================================================================
    // KERN-OBJ API IMPLEMENTATIONS
    // ===================================================================
    
    /// <summary>
    /// (kern-obj-put-at obj (place x y))
    /// Places an object at a location. Location is a list.
    /// </summary>
    public static object ObjectPutAt(object obj, object location)
    {
        Console.WriteLine($"  ObjectPutAt called:");
        Console.WriteLine($"    obj type: {obj?.GetType().Name ?? "NULL"}");
        Console.WriteLine($"    location type: {location?.GetType().Name ?? "NULL"}");

        var gameObj = obj as Object;
    
        if (gameObj == null)
        {
            return Builtins.Unspecified;
        }
    
        if (location is Cons locList)
        {
            Console.WriteLine($"    locList.car type: {locList.car?.GetType().Name ?? "NULL"}");
            Console.WriteLine($"    locList.cdr type: {locList.cdr?.GetType().Name ?? "NULL"}");

            var place = locList.car as Place;
            var rest = locList.cdr as Cons;
        
            if (place == null)
            {
                return Builtins.Unspecified;
            }
        
            if (rest != null)
            {
                int x = Convert.ToInt32(rest.car ?? 0);
                var rest2 = rest.cdr as Cons;
                int y = rest2 != null ? Convert.ToInt32(rest2.car ?? 0) : 0;
                
                Console.WriteLine($"    Placing at: {place.Name} ({x}, {y})");
            
                // Verify position BEFORE.
                var posBefore = gameObj.GetPosition();
                Console.WriteLine($"    Position BEFORE: Place={posBefore?.Place?.Name ?? "NULL"}, X={posBefore?.X}, Y={posBefore?.Y}");
            
                place.AddObject(gameObj, x, y);
            
                // Verify position AFTER.
                var posAfter = gameObj.GetPosition();
                Console.WriteLine($"    Position AFTER: Place={posAfter?.Place?.Name ?? "NULL"}, X={posAfter?.X}, Y={posAfter?.Y}");
            }
        }
        else
        {
            Console.WriteLine($"    [ERROR] location is not a Cons!");
        }
    
        return Builtins.Unspecified;
    }

    /// <summary>
    /// (kern-obj-get-name object)
    /// Gets the name of an object.
    /// </summary>
    public static object ObjectGetName(object obj)
    {
        if (obj is Character character)
            return character.GetName();
        else if (obj is Object gameObj)
            return gameObj.Name;
        else if (obj is ObjectType objType)
            return objType.Name;
    
        return "(unnamed)";
    }
    
    public static object ObjectGetLocation(object args)
    {
        // TODO: Implement
        return Builtins.Unspecified;
    }
    
    /// <summary>
    /// (kern-obj-get-conversation obj)
    /// Get the conversation closure attached to a character.
    /// </summary>
    public static object ObjectGetConversation(object obj)
    {
        if (obj is Character character)
        {
            return character.Conversation ?? "#f".Eval();
        }

        return "#f".Eval();
    }
    
    /// <summary>
    /// (kern-obj-apply-damage obj description amount)
    /// Applies damage to an object.
    /// For Beings (characters, etc.), reduces HP.
    /// For other objects, runs the damage hook if present.
    /// Matches Nazghul's kern-obj-apply-damage.
    /// </summary>
    public static object ObjectApplyDamage(object obj, object desc, object amount)
    {
        if (obj == null)
        {
            Console.WriteLine("[ERROR] kern-obj-apply-damage: null object");
            return null;
        }
    
        int dmg = Convert.ToInt32(amount);
        string description = desc?.ToString() ?? "damage";
    
        // If it's a Being (Character, etc.), it has HP and can take damage.
        if (obj is Being being)
        {
            being.Damage(dmg);
            Console.WriteLine($"{being.GetName()} takes {dmg} {description}");
            return null;
        }
    
        // For other Objects, we'd run the damage hook if implemented.
        // For now, just log it.
        if (obj is Object gameObj)
        {
            // TODO: Run OBJ_HOOK_DAMAGE when hook system is implemented.
            // gameObj.RunHook(ObjectHook.Damage);
            Console.WriteLine($"Object takes {dmg} {description}");
        }
    
        return null;
    }
}
