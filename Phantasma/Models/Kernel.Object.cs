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
    
    /// <summary>
    /// (kern-obj-add-effect object effect gob)
    /// Adds an effect to an object.
    /// </summary>
    public static object ObjectAddEffect(object obj, object effect, object gob)
    {
        if (obj is not Object gameObj)
        {
            RuntimeError("kern-obj-add-effect: not a game object");
            return false;
        }
        
        if (effect is not Effect eff)
        {
            RuntimeError("kern-obj-add-effect: not an effect");
            return false;
        }
        
        gameObj.AddEffect(eff, gob);
        return true;
    }
    
    /// <summary>
    /// (kern-obj-remove-effect object effect)
    /// Removes an effect from an object.
    /// </summary>
    public static object ObjectRemoveEffect(object obj, object effect)
    {
        if (obj is not Object gameObj)
        {
            RuntimeError("kern-obj-remove-effect: not a game object");
            return false;
        }
        
        if (effect is not Effect eff)
        {
            RuntimeError("kern-obj-remove-effect: not an effect");
            return false;
        }
        
        gameObj.RemoveEffect(eff);
        return true;
    }
    
    /// <summary>
    /// (kern-obj-has-effect? object effect)
    /// Checks if an object has a specific effect.
    /// </summary>
    public static object ObjectHasEffect(object obj, object effect)
    {
        if (obj is not Object gameObj)
        {
            RuntimeError("kern-obj-has-effect?: not a game object");
            return false;
        }
        
        if (effect is not Effect eff)
        {
            RuntimeError("kern-obj-has-effect?: not an effect");
            return false;
        }
        
        return gameObj.HasEffect(eff);
    }
    
    /// <summary>
    /// (kern-obj-remove object)
    /// Removes an object from the map.
    /// </summary>
    public static object ObjectRemove(object obj)
    {
        if (obj is not Object gameObj)
        {
            RuntimeError("kern-obj-remove: not a game object");
            return Builtins.Unspecified;
        }
        
        // Remove from place.
        var place = gameObj.Position?.Place;
        if (place != null)
        {
            place.RemoveObject(gameObj);
        }
        
        return Builtins.Unspecified;
    }
    
    /// <summary>
    /// (kern-obj-relocate obj location [cutscene])
    /// Moves an object to a new location, optionally running a cutscene.
    /// Location is a list: (place x y)
    /// Cutscene can be a closure or #f/nil for no cutscene.
    /// </summary>
    public static object ObjectRelocate(object obj, object location, object cutscene)
    {
        if (obj is not Object gameObj)
        {
            Console.WriteLine("[WARNING] kern-obj-relocate: null or invalid object");
            return "#f".Eval();
        }
    
        // Unpack location list: (place x y)
        if (location is not Cons locList)
        {
            Console.WriteLine("[WARNING] kern-obj-relocate: location must be a list");
            return "#f".Eval();
        }
    
        var place = locList.car as Place;
        var rest = locList.cdr as Cons;
    
        if (place == null || rest == null)
        {
            Console.WriteLine("[WARNING] kern-obj-relocate: invalid location format");
            return "#f".Eval();
        }
    
        int x = Convert.ToInt32(rest.car);
        var rest2 = rest.cdr as Cons;
        int y = rest2 != null ? Convert.ToInt32(rest2.car) : 0;
    
        // Get cutscene closure if provided (ignore #f, nil, etc.).
        Callable? cutsceneCallable = cutscene as Callable;
    
        // Perform the relocation.
        gameObj.Relocate(place, x, y, cutsceneCallable);
        
        return "#t".Eval();
    }
    
    /// <summary>
    /// (kern-obj-find-path obj place x y)
    /// Find path from object's current location to destination.
    /// Returns Scheme list of (x y) pairs, or nil if no path.
    /// </summary>
    public static object ObjectFindPath(object objArg, object placeArg, object xArg, object yArg)
    {
        if (objArg is not Object obj)
        {
            Console.WriteLine("[kern-obj-find-path] Invalid object");
            return false;
        }

        if (placeArg is not Place place)
        {
            Console.WriteLine("[kern-obj-find-path] Invalid place");
            return false;
        }

        // Can't pathfind between places.
        if (obj.GetPlace() != place)
        {
            Console.WriteLine("[kern-obj-find-path] Object not in target place");
            return false;
        }

        int destX = Convert.ToInt32(xArg);
        int destY = Convert.ToInt32(yArg);

        // Find the path.
        var path = AStar.Search(
            obj.GetX(), obj.GetY(),
            destX, destY,
            place.Width, place.Height,
            (x, y) => place.IsPassable(x, y, obj)
        );

        if (path == null || path.Count == 0)
            return false;

        // Convert to Scheme list of (x y) pairs.
        return ConvertPathToSchemeList(path);
    }

    /// <summary>
    /// (kern-obj-is-visible? obj)
    /// Check if object is visible.
    /// </summary>
    public static object ObjIsVisible(object objArg)
    {
        if (objArg is not Object obj)
        {
            Console.WriteLine("[kern-obj-is-visible?] Invalid object");
            return false;
        }

        return obj.IsVisible();
    }
    
    /// <summary>
    /// (kern-obj-wander obj)
    /// Make object wander in a random direction.
    /// </summary>
    public static object ObjectWander(object objArg)
    {
        if (objArg is not Being being)
        {
            Console.WriteLine("[kern-obj-wander] Object is not a being");
            return false;
        }

        var random = new Random();
        int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
        int[] dy = { -1, -1, 0, 1, 1, 1, 0, -1 };

        int startDir = random.Next(8);

        for (int i = 0; i < 8; i++)
        {
            int dir = (startDir + i) % 8;
            int newX = being.GetX() + dx[dir];
            int newY = being.GetY() + dy[dir];

            if (being.CanWanderTo(newX, newY))
            {
                being.Move(dx[dir], dy[dir]);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// (kern-obj-is-visible? obj)
    /// </summary>
    public static object ObjectIsVisible(object objArg)
    {
        if (objArg is not Being being) return true; // Non-beings default visible
        return being.IsVisible();
    }

    /// <summary>
    /// (kern-obj-move obj dx dy)
    /// </summary>
    public static object ObjectMove(object objArg, object dxArg, object dyArg)
    {
        if (objArg is not Being being) return false;
        return being.Move(Convert.ToInt32(dxArg), Convert.ToInt32(dyArg));
    }

    /// <summary>
    /// (kern-obj-get-ap obj)
    /// </summary>
    public static object ObjectGetActionPoints(object objArg)
    {
        if (objArg is not Being being) return 0;
        return being.ActionPoints;
    }

    /// <summary>
    /// (kern-obj-set-ap obj ap)
    /// </summary>
    public static object ObjectSetActionPoints(object objArg, object apArg)
    {
        if (objArg is not Being being) return false;
        being.ActionPoints = Convert.ToInt32(apArg);
        return being;
    }

    /// <summary>
    /// (kern-obj-dec-ap obj amount)
    /// </summary>
    public static object ObjectDecreaseActionPoints(object objArg, object amountArg)
    {
        if (objArg is not Being being) return false;
        being.ActionPoints = Math.Max(0, being.ActionPoints - Convert.ToInt32(amountArg));
        return being;
    }

    /// <summary>
    /// (kern-obj-is-being? obj)
    /// </summary>
    public static object ObjectIsBeing(object objArg)
    {
        return objArg is Being;
    }
}
