using System;
using IronScheme;
using IronScheme.Runtime;
using IronScheme.Scripting;

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
        // Resolve the object (might be a Character, Object, etc.).
        var gameObj = obj as Object;
        
        if (gameObj == null)
        {
            // Try to resolve from tag if it's a string.
            if (obj is string objTag)
            {
                gameObj = Phantasma.GetRegisteredObject(objTag) as Object;
            }
        }
        
        if (gameObj == null)
        {
            Console.WriteLine($"[kern-obj-put-at] Error: null or invalid object");
            return "nil".Eval();
        }
        
        if (location is Cons locList)
        {
            // The place might be a Place object directly, or a string tag.
            Place place = null;
            
            if (locList.car is Place p)
            {
                place = p;
            }
            else if (locList.car is string placeTag)
            {
                // Look up the place by its tag.
                place = Phantasma.GetRegisteredObject(placeTag) as Place;
                
                if (place == null)
                {
                    // Try with quote prefix stripped.
                    string cleanTag = placeTag.TrimStart('\'').Trim('"');
                    place = Phantasma.GetRegisteredObject(cleanTag) as Place;
                }
                
                if (place == null)
                {
                    Console.WriteLine($"[kern-obj-put-at] Error: could not resolve place tag '{placeTag}'");
                    return "nil".Eval();
                }
            }
            else
            {
                Console.WriteLine($"[kern-obj-put-at] Error: place is neither Place nor string (type: {locList.car?.GetType().Name ?? "NULL"})");
                return "nil".Eval();
            }
            
            // Extract coordinates.
            var rest = locList.cdr as Cons;
            
            if (rest == null)
            {
                Console.WriteLine($"[kern-obj-put-at] Error: missing coordinates in location list");
                return "nil".Eval();
            }
            
            int x = Convert.ToInt32(rest.car ?? 0);
            var rest2 = rest.cdr as Cons;
            int y = rest2 != null ? Convert.ToInt32(rest2.car ?? 0) : 0;
            
            // Place the object.
            Console.WriteLine($"[kern-obj-put-at] Placing {gameObj.Name} at {place.Name} ({x}, {y})");
            
            place.AddObject(gameObj, x, y);
            
            // Verify placement worked.
            var posAfter = gameObj.GetPosition();
            if (posAfter?.Place == null)
            {
                Console.WriteLine($"[kern-obj-put-at] WARNING: Object position not set after AddObject!");
            }
        }
        else
        {
            Console.WriteLine($"[kern-obj-put-at] Error: location is not a list (type: {location?.GetType().Name ?? "NULL"})");
        }
        
        return "nil".Eval();
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
    
    // ============================================================
    // kern-obj-get-type - Gets the ObjectType of an object
    // ============================================================
    public static object ObjectGetType(object obj)
    {
        if (obj == null || IsNil(obj))
            return RuntimeHelpers.False;  // Return #f as nil.
    
        if (obj is Item item)
            return (object?)item.Type ?? RuntimeHelpers.False;
    
        if (obj is Character)
            return RuntimeHelpers.False;  // Characters have no ObjectType.
    
        if (obj is Being)
            return RuntimeHelpers.False;
    
        // Try to resolve by tag.
        if (obj is string tag)
        {
            var resolved = Phantasma.GetRegisteredObject(tag);
            if (resolved is Item i)
                return (object?)i.Type ?? RuntimeHelpers.False;
        }
    
        return RuntimeHelpers.False;
    }
    
    public static object ObjectGetLocation(object args)
    {
        // TODO: Implement
        return "nil".Eval();
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
            return "nil".Eval();
        }
        
        // Remove from place.
        var place = gameObj.Position?.Place;
        if (place != null)
        {
            place.RemoveObject(gameObj);
        }
        
        return "nil".Eval();
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

    // ============================================================
    // kern-obj-set-visible
    // ============================================================
    public static object ObjectSetVisible(object obj, object visible)
    {
        if (obj == null || IsNil(obj))
        {
            Console.WriteLine("[kern-obj-set-visible] null object");
            return "nil".Eval();
        }
        
        bool val = Convert.ToBoolean(visible);
        
        // Try direct Being.
        if (obj is Being being)
        {
            being.SetVisible(val);
            return obj;
        }
        
        // Try Item.
        if (obj is Item item)
        {
            item.SetVisible(val);
            return obj;
        }
        
        // Try to resolve by tag.
        if (obj is string tag)
        {
            var resolved = Phantasma.GetRegisteredObject(tag);
            if (resolved is Being b)
            {
                b.SetVisible(val);
                return resolved;
            }
            if (resolved is Item i)
            {
                i.SetVisible(val);
                return resolved;
            }
        }
        
        return obj;
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
    
    /// <summary>
    /// (kern-obj-get-gob obj)
    /// Returns the Scheme data attached to an object via its gob.
    /// 
    /// This is how scripts access quest state, NPC memory, etc.
    /// Returns the raw Scheme data (list, pair, symbol, etc.) that
    /// was previously attached via kern-obj-set-gob.
    /// </summary>
    /// <param name="objArg">The game object to get the gob from</param>
    /// <returns>The Scheme data, or NIL if no gob attached</returns>
    public static object ObjectGetGob(object objArg)
    {
        // Handle the object argument.
        Object? obj = objArg as Object;
        
        if (obj == null)
        {
            RuntimeError("kern-obj-get-gob: bad args");
            return "nil".Eval();
        }
        
        if (obj.Gob == null || obj.Gob?.SchemeData == null)
        {
            RuntimeError($"kern-obj-get-gob: no gob for {obj.Name ?? "unknown"}");
            return "nil".Eval();
        }
        
        // Return the Scheme data directly - it's already an IronScheme object.
        return obj.Gob?.SchemeData;
    }
    
    /// <summary>
    /// (kern-obj-set-gob obj gob-data)
    /// Attaches Scheme data to an object via a gob.
    /// 
    /// The gob-data can be any Scheme value: a list for complex state,
    /// a symbol, a number, etc. This data persists with the object
    /// and is saved/loaded with the game.
    /// 
    /// </summary>
    /// <param name="objArg">The game object to attach gob to</param>
    /// <param name="gobData">The Scheme data to attach</param>
    /// <returns>Unspecified</returns>
    public static object ObjectSetGob(object objArg, object gobData)
    {
        // Handle null object argument.
        if (objArg == null)
        {
            Console.WriteLine("[RUNTIME ERROR] kern-obj-set-gob: null object");
            return "nil".Eval();
        }
        
        // Check for Unspecified (this is what happens when kern-mk-obj fails).
        if (objArg == "nil".Eval())
        {
            Console.WriteLine("[RUNTIME ERROR] kern-obj-set-gob: object is #<unspecified> (kern-mk-obj may have failed)");
            return "nil".Eval();
        }
        
        // Try to get the object directly or by tag.
        Object obj = null;
        
        if (objArg is Object directObj)
        {
            obj = directObj;
        }
        else if (objArg is string tagStr)
        {
            string cleanTag = tagStr.TrimStart('\'').Trim('"');
            var resolved = Phantasma.GetRegisteredObject(cleanTag);
            if (resolved is Object resolvedObj)
                obj = resolvedObj;
        }
        else
        {
            // Try ToTag for symbols.
            string tag = ToTag(objArg);
            if (!string.IsNullOrEmpty(tag))
            {
                var resolved = Phantasma.GetRegisteredObject(tag);
                if (resolved is Object resolvedObj)
                    obj = resolvedObj;
            }
        }
        
        if (obj == null)
        {
            Console.WriteLine($"[RUNTIME ERROR] kern-obj-set-gob: bad args (got {objArg?.GetType().Name ?? "null"})");
            return "nil".Eval();
        }
        
        // Handle null gob data.
        if (gobData == null || IsNil(gobData))
        {
            obj.Gob = null;
            return "nil".Eval();
        }
        
        // Create a new Gob with the Scheme data.
        obj.Gob = new Gob(gobData)
        {
            Flags = Gob.GOB_SAVECAR
        };
        
        return "nil".Eval();
    }
}
