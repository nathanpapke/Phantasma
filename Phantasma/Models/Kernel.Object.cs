using System;
using System.Collections.Generic;
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
    public static object ObjectPutAt(object[] args)
    {
        if (args == null || args.Length < 2)
        {
            Console.WriteLine($"[kern-obj-put-at] Expected 2 args (obj loc), got {args?.Length ?? 0}");
            return "nil".Eval();
        }
        
        object obj = args[0];
        object location = args[1];
        
        Console.WriteLine($"[kern-obj-put-at] Received obj type: {obj?.GetType().FullName ?? "NULL"}");
        
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
        
        if (!UnpackLocation(location, out var place, out int x, out int y))
        {
            Console.WriteLine($"[kern-obj-put-at] Error: invalid location (type: {location?.GetType().Name ?? "NULL"})");
            return "nil".Eval();
        }
        
        // Set position and add to place.
        gameObj.SetPosition(place, x, y);
        place.AddObject(gameObj, x, y);
        
        // After successfully placing the object, send 'init signal.
        if (gameObj != null)
        {
            // Call the object's 'init handler if it has one.
            var ifc = gameObj.Type?.InteractionHandler;
            if (ifc is Callable callable)
            {
                try
                {
                    Console.WriteLine($"[kern-obj-put-at] Sending 'init to {gameObj.Name}");
                    var initSymbol = SymbolTable.StringToObject("init");
                    callable.Call(initSymbol, gameObj);
                }
                catch (Exception ex)
                {
                    // 'init handler may not exist for all objects, that's okay.
                    Console.WriteLine($"[kern-obj-put-at] No init handler or error: {ex.Message}");
                }
            }
        }
        
        return "nil".Eval();
    }

    /// <summary>
    /// (kern-obj-get-name object)
    /// Gets the name of an object.
    /// </summary>
    public static object ObjectGetName(object obj)
    {
        // Handle variadic array wrapper from IronScheme.
        if (obj is object[] arr && arr.Length > 0)
            obj = arr[0];
        
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
        // Handle variadic array wrapper from IronScheme.
        if (obj is object[] arr && arr.Length > 0)
            obj = arr[0];
        
        if (obj == null || IsNil(obj))
            return RuntimeHelpers.False;
        
        // Characters don't have ObjectTypes in Nazghul.
        if (obj is Character)
            return RuntimeHelpers.False;
        
        // Beings also don't have ObjectTypes (they have Species instead).
        if (obj is Being)
            return RuntimeHelpers.False;
        
        // Handle other Objects (Mechanism, Item, TerrainFeature, etc.).
        if (obj is Object gameObj)
            return (object?)gameObj.Type ?? RuntimeHelpers.False;
        
        // Try to resolve by tag.
        if (obj is string tag)
        {
            var resolved = Phantasma.GetRegisteredObject(tag);
            if (resolved is Object resolvedObj && !(resolvedObj is Being))
                return (object?)resolvedObj.Type ?? RuntimeHelpers.False;
        }
        
        return RuntimeHelpers.False;
    }
    
    /// <summary>
    /// (kern-obj-get-location obj)
    /// Gets the location of an object as a list: (place x y)
    /// Returns nil if object has no location.
    /// </summary>
    public static object ObjectGetLocation(object[] args)
    {
        object objArg = args != null && args.Length > 0 ? args[0] : null;
        
        // Handle Cons list wrapping.
        if (objArg is Cons cons)
            objArg = cons.car;
        
        Console.WriteLine($"[kern-obj-get-location] Called with: {objArg?.GetType().Name ?? "NULL"}");
        
        if (objArg == null || IsNil(objArg))
        {
            Console.WriteLine("[kern-obj-get-location] ERROR: null object");
            return "nil".Eval();
        }
        
        // Extract place and coordinates from the object.
        Place place = null;
        int x = 0, y = 0;
        
        if (objArg is Character character)
        {
            place = character.GetPlace();
            x = character.GetX();
            y = character.GetY();
            Console.WriteLine($"[kern-obj-get-location] Character '{character.GetName()}' at ({x}, {y}) in {place?.Name ?? "NULL"}");
        }
        else if (objArg is Being being)
        {
            place = being.GetPlace();
            x = being.GetX();
            y = being.GetY();
            Console.WriteLine($"[kern-obj-get-location] Being at ({x}, {y}) in {place?.Name ?? "NULL"}");
        }
        else if (objArg is Object gameObj)
        {
            var pos = gameObj.GetPosition();
            if (pos != null)
            {
                place = pos.Place;
                x = pos.X;
                y = pos.Y;
            }
            Console.WriteLine($"[kern-obj-get-location] Object '{gameObj.Name}' at ({x}, {y}) in {place?.Name ?? "NULL"}");
        }
        else
        {
            // Try to resolve by tag.
            string tag = ToTag(objArg);
            Console.WriteLine($"[kern-obj-get-location] Trying tag lookup: '{tag}'");
            
            if (!string.IsNullOrEmpty(tag))
            {
                var resolved = Phantasma.GetRegisteredObject(tag);
                
                if (resolved is Character ch)
                {
                    place = ch.GetPlace();
                    x = ch.GetX();
                    y = ch.GetY();
                }
                else if (resolved is Being b)
                {
                    place = b.GetPlace();
                    x = b.GetX();
                    y = b.GetY();
                }
                else if (resolved is Object obj)
                {
                    var pos = obj.GetPosition();
                    if (pos != null)
                    {
                        place = pos.Place;
                        x = pos.X;
                        y = pos.Y;
                    }
                }
            }
        }
        
        // Return nil if object has no place.
        if (place == null)
        {
            Console.WriteLine($"[kern-obj-get-location] Object has no place, returning nil");
            return "nil".Eval();
        }
        
        Console.WriteLine($"[kern-obj-get-location] Returning location: ({place.Name}, {x}, {y})");
        
        // Return as Scheme list: (place x y)
        // Build Cons list that kern-obj-put-at can parse.
        // Format: Cons(place, Cons(x, Cons(y, null)))
        return new Cons(place, new Cons(x, new Cons(y, null)));
    }
    
    /// <summary>
    /// (kern-obj-get-conversation obj)
    /// Get the conversation closure attached to a character.
    /// </summary>
    public static object ObjectGetConversation(object obj)
    {
        // Handle variadic array wrapper from IronScheme.
        if (obj is object[] arr && arr.Length > 0)
            obj = arr[0];
        
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
    public static object ObjectApplyDamage(object[] args)
    {
        if (args == null || args.Length < 3)
        {
            Console.WriteLine($"[kern-obj-apply-damage] Expected 3 args, got {args?.Length ?? 0}");
            return null;
        }
        
        object obj = args[0];
        object desc = args[1];
        object amount = args[2];
        
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
        // Handle variadic array wrapper from IronScheme.
        if (obj is object[] arr && arr.Length >= 3)
        {
            gob = arr[2];
            effect = arr[1];
            obj = arr[0];
        }
        
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
    /// (kern-obj-get-effects obj)
    /// Returns a Scheme list of all Effect objects attached to this object,
    /// across all hook types (start-of-turn, add-hook, damage, keystroke).
    /// </summary>
    public static object ObjectGetEffect(object objArg)
    {
        // Handle variadic array wrapper from IronScheme.
        if (objArg is object[] arr && arr.Length > 0)
            objArg = arr[0];
        
        // Accept any Object (Being, Character, Item, etc.).
        var obj = objArg as Object;
        if (obj == null)
        {
            // Try resolving by tag if it's a string.
            if (objArg is string tag)
                obj = Phantasma.GetRegisteredObject(tag) as Object;
            
            if (obj == null)
            {
                RuntimeError("kern-obj-get-effects: not a valid object");
                return "'()".Eval();
            }
        }
        
        // Collect all effects across all hook types.
        var effects = new List<object>();
        
        for (int hookId = 0; hookId < (int)HookId.NumHooks; hookId++)
        {
            obj.HookForEach((HookId)hookId, hook =>
            {
                if (hook.Effect != null)
                {
                    effects.Add(hook.Effect);
                }
            });
        }
        
        // Build Scheme list (right-fold to preserve order).
        // Return null/'() for empty, proper Cons chain for results.
        if (effects.Count == 0)
            return "'()".Eval();
        
        // Build the list from back to front.
        object result = "'()".Eval();
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            result = new Cons(effects[i], result);
        }
        
        return result;
    }
    
    /// <summary>
    /// (kern-obj-remove-effect object effect)
    /// Removes an effect from an object.
    /// </summary>
    public static object ObjectRemoveEffect(object obj, object effect)
    {
        // Handle variadic array wrapper from IronScheme.
        if (obj is object[] arr && arr.Length >= 2)
        {
            effect = arr[1];
            obj = arr[0];
        }
        
        if (obj is not Object gameObj)
        {
            RuntimeError("kern-obj-remove-effect: not a game object");
            return "#f".Eval();
        }
        
        if (effect is not Effect eff)
        {
            RuntimeError("kern-obj-remove-effect: not an effect");
            return "#f".Eval();
        }
        
        gameObj.RemoveEffect(eff);
        return "#t".Eval();
    }
    
    /// <summary>
    /// (kern-obj-has-effect? object effect)
    /// Checks if an object has a specific effect.
    /// </summary>
    public static object ObjectHasEffect(object obj, object effect)
    {
        // Handle variadic array wrapper from IronScheme.
        if (obj is object[] arr && arr.Length >= 2)
        {
            effect = arr[1];
            obj = arr[0];
        }
        
        if (obj is not Object gameObj)
        {
            RuntimeError("kern-obj-has-effect?: not a game object");
            return "#f".Eval();
        }
        
        if (effect is not Effect eff)
        {
            RuntimeError("kern-obj-has-effect?: not an effect");
            return "#f".Eval();
        }

        return gameObj.HasEffect(eff) ? "#t".Eval() : "#f".Eval();
    }
    
    /// <summary>
    /// (kern-obj-remove object)
    /// Removes an object from the map.
    /// </summary>
    public static object ObjectRemove(object objArg)
    {
        // Handle variadic array wrapper from IronScheme.
        if (objArg is object[] arr && arr.Length > 0)
            objArg = arr[0];
        
        Object? obj = objArg as Object;

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
    public static object ObjectRelocate(object[] args)
    {
        if (args == null || args.Length < 2)
        {
            Console.WriteLine($"[kern-obj-relocate] Expected 2-3 args, got {args?.Length ?? 0}");
            return "#f".Eval();
        }
        
        if (args[0] is not Object gameObj)
        {
            Console.WriteLine("[WARNING] kern-obj-relocate: null or invalid object");
            return "#f".Eval();
        }
        
        if (!UnpackLocation(args[1], out var place, out int x, out int y))
        {
            Console.WriteLine("[WARNING] kern-obj-relocate: invalid location list");
            return "#f".Eval();
        }
    
        // Get cutscene closure if provided (ignore #f, nil, etc.).
        Callable? cutsceneCallable = args.Length >= 3 ? args[2] as Callable : null;
        
        // Perform the relocation.
        gameObj.Relocate(place, x, y, cutsceneCallable);
        
        return "#t".Eval();
    }
    
    /// <summary>
    /// (kern-obj-find-path obj place x y)
    /// Find path from object's current location to destination.
    /// Returns Scheme list of (x y) pairs, or nil if no path.
    /// </summary>
    public static object ObjectFindPath(object[] args)
    {
        if (args == null || args.Length < 2)
        {
            Console.WriteLine($"[kern-obj-find-path] Expected 2 args (obj loc), got {args?.Length ?? 0}");
            return "#f".Eval();
        }
        
        if (args[0] is not Object obj)
        {
            Console.WriteLine("[kern-obj-find-path] Invalid object");
            return "#f".Eval();
        }
        
        if (!UnpackLocation(args[1], out var place, out int x, out int y))
        {
            Console.WriteLine("[kern-obj-find-path] Invalid location list");
            return "#f".Eval();
        }
        
        // Can't pathfind between places.
        if (obj.GetPlace() != place)
        {
            Console.WriteLine("[kern-obj-find-path] Object not in target place");
            return "#f".Eval();
        }
        
        // Find the path.
        var path = AStar.Search(
            obj.GetX(), obj.GetY(),
            x, y,
            place.Width, place.Height,
            (px, py) => place.IsPassable(px, py, obj)
        );

        if (path == null || path.Count == 0)
            return "#f".Eval();

        // Convert to Scheme list of (x y) pairs.
        return ConvertPathToSchemeList(path);
    }

    // ============================================================
    // kern-obj-set-visible
    // ============================================================
    public static object ObjectSetVisible(object obj, object visible)
    {
        // Handle variadic array wrapper from IronScheme.
        if (obj is object[] arr && arr.Length >= 2)
        {
            visible = arr[1];
            obj = arr[0];
        }
        
        if (obj == null || IsNil(obj))
        {
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
        // Handle variadic array wrapper from IronScheme.
        if (objArg is object[] arr && arr.Length > 0)
            objArg = arr[0];
        
        if (objArg is not Being being)
        {
            Console.WriteLine("[kern-obj-wander] Object is not a being");
            return "#f".Eval();
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
                return "#t".Eval();
            }
        }

        return "#f".Eval();
    }

    /// <summary>
    /// (kern-obj-is-visible? obj)
    /// </summary>
    public static object ObjectIsVisible(object objArg)
    {
        // Handle variadic array wrapper from IronScheme.
        if (objArg is object[] arr && arr.Length > 0)
            objArg = arr[0];
        
        if (objArg is not Being being) return true; // Non-beings default visible
        return being.IsVisible();
    }

    /// <summary>
    /// (kern-obj-move obj dx dy)
    /// </summary>
    public static object ObjectMove(object[] args)
    {
        if (args == null || args.Length < 3)
        {
            Console.WriteLine($"[kern-obj-move] Expected 3 args, got {args?.Length ?? 0}");
            return false;
        }
        
        if (args[0] is not Being being) return false;
        return being.Move(Convert.ToInt32(args[1]), Convert.ToInt32(args[2]));
    }

    /// <summary>
    /// (kern-obj-get-ap obj)
    /// </summary>
    public static object ObjectGetActionPoints(object[] args)
    {
        if (args == null || args.Length < 1) return 0;
        
        if (args[0] is not Being being) return 0;
        return being.ActionPoints;
    }

    /// <summary>
    /// (kern-obj-set-ap obj ap)
    /// </summary>
    public static object ObjectSetActionPoints(object[] args)
    {
        if (args == null || args.Length < 2) return false;
        
        if (args[0] is not Being being) return false;
        being.ActionPoints = Convert.ToInt32(args[1]);
        return being;
    }

    /// <summary>
    /// (kern-obj-dec-ap obj amount)
    /// </summary>
    public static object ObjectDecreaseActionPoints(object[] args)
    {
        if (args == null || args.Length < 2) return false;
        
        if (args[0] is not Being being) return false;
        being.ActionPoints = Math.Max(0, being.ActionPoints - Convert.ToInt32(args[1]));
        return being;
    }

    /// <summary>
    /// (kern-obj-is-being? obj)
    /// </summary>
    public static object ObjectIsBeing(object objArg)
    {
        // Handle variadic array wrapper from IronScheme.
        if (objArg is object[] arr && arr.Length > 0)
            objArg = arr[0];

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
        // Handle variadic array wrapper from IronScheme.
        if (objArg is object[] arr && arr.Length > 0)
            objArg = arr[0];
        
        // Handle the object argument.
        Object? obj = objArg as Object;
        
        // If not a direct Object, try string tag lookup.
        if (obj == null && objArg is string tag)
        {
            string cleanTag = tag.TrimStart('\'').Trim('"');
            var resolved = Phantasma.GetRegisteredObject(cleanTag);
            obj = resolved as Object;
        }
        
        // Try ToTag for symbols.
        if (obj == null)
        {
            string tagStr = ToTag(objArg);
            if (!string.IsNullOrEmpty(tagStr))
            {
                var resolved = Phantasma.GetRegisteredObject(tagStr);
                obj = resolved as Object;
            }
        }
        
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
        // Handle case where both args come bundled in objArg as array.
        if (objArg is object[] arr && arr.Length >= 2)
        {
            objArg = arr[0];
            gobData = arr[1];
        }
        
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
        
        if (obj.Type != null && obj.Type.CanExec && obj.Type.InteractionHandler != null)
        {
            if (obj.Type.InteractionHandler is Callable callable)
            {
                // Try calling with "exec" string.
                var execSymbol = SymbolTable.StringToObject("exec");
                var result = callable.Call(execSymbol, obj);
            }
        }
        
        return "nil".Eval();
    }
    
    /// <summary>
    /// (kern-obj-set-pclass obj pclass)
    /// Sets the passability class of an object.
    /// </summary>
    public static object ObjectSetPassability(object obj, object pclass)
    {
        // Handle case where both args come bundled in obj as array.
        if (obj is object[] arr && arr.Length >= 2)
        {
            obj = arr[0];
            pclass = arr[1];
        }
        
        var gameObj = obj as Object;
        if (gameObj == null)
        {
            return "nil".Eval();
        }
    
        int pclassValue = ToInt(pclass, 0);
        gameObj.PassabilityClass = pclassValue;
        
        return "nil".Eval();
    }
    
    /// <summary>
    /// (kern-obj-get-sprite obj)
    /// Gets the sprite of an object.
    /// </summary>
    public static object ObjectGetSprite(object objArg)
    {
        // Handle variadic array wrapper from IronScheme.
        if (objArg is object[] arr && arr.Length > 0)
            objArg = arr[0];
        
        // Handle null object argument.
        if (objArg == null)
        {
            Console.WriteLine("[RUNTIME ERROR] kern-obj-get-sprite: null object");
            return "nil".Eval();
        }
        
        // Check for Unspecified.
        if (objArg == "nil".Eval())
        {
            Console.WriteLine("[RUNTIME ERROR] kern-obj-get-sprite: object is #<unspecified>");
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
            Console.WriteLine($"[RUNTIME ERROR] kern-obj-get-sprite: bad object (got " +
                              $"{objArg?.GetType().Name ?? "null"})");
            return "nil".Eval();
        }
        
        if (obj.Sprite == null)
        {
            return "nil".Eval();
        }
        
        // Return the sprite object itself (or its tag if needed for Scheme).
        // The original Nazghul returns the sprite pointer, so we return the Sprite object.
        return obj.Sprite;
    }
    
    /// <summary>
    /// (kern-obj-set-sprite obj sprite)
    /// Sets the sprite of an object.
    /// </summary>
    public static object ObjectSetSprite(object objArg, object spriteArg)
    {
        // Handle variadic array wrapper from IronScheme.
        if (objArg is object[] arr && arr.Length >= 2)
        {
            spriteArg = arr[1];
            objArg = arr[0];
        }
        
        // Handle null object argument.
        if (objArg == null)
        {
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
            return "nil".Eval();
        }
        
        // Handle null/empty sprite.
        if (spriteArg == null || IsNil(spriteArg))
        {
            obj.Sprite = null;
            return "nil".Eval();
        }
        
        // Try to get the sprite directly or by tag.
        Sprite sprite = null;
        
        if (spriteArg is Sprite directSprite)
        {
            sprite = directSprite;
        }
        else if (spriteArg is string spriteTagStr)
        {
            string cleanTag = spriteTagStr.TrimStart('\'').Trim('"');
            
            var resolved = Phantasma.GetRegisteredObject(cleanTag);
            
            if (resolved is Sprite resolvedSprite)
            {
                sprite = resolvedSprite;
            }
        }
        else
        {
            // Try ToTag for symbols.
            string spriteTag = ToTag(spriteArg);
            
            if (!string.IsNullOrEmpty(spriteTag))
            {
                var resolved = Phantasma.GetRegisteredObject(spriteTag);
                if (resolved is Sprite resolvedSprite)
                {
                    sprite = resolvedSprite;
                }
            }
        }
        
        if (sprite == null)
        {
            return "nil".Eval();
        }
        
        // Set the new sprite.
        obj.Sprite = sprite;
        
        return "nil".Eval();
    }
    
    /// <summary>
    /// (kern-obj-set-opacity)
    /// </summary>
    /// <param name="objArg">Object to set opacity</param>
    /// <param name="opacityArg">Opacity #t or #f</param>
    public static object ObjectSetOpacity(object objArg, object opacityArg)
    {
        // Handle variadic array wrapper from IronScheme.
        if (objArg is object[] arr && arr.Length >= 2)
        {
            objArg = arr[0];
            opacityArg = arr[1];
        }
    
        var obj = objArg as Object;
        if (obj == null)
        {
            return "nil".Eval();
        }
    
        bool isOpaque = ToBool(opacityArg, false);
        obj.IsOpaque = isOpaque;
    
        return "nil".Eval();
    }
    
    /// <summary>
    /// (kern-obj-set-light obj light)
    /// Sets the light radius emitted by an object.
    /// </summary>
    public static object ObjectSetLight(object obj, object light)
    {
        // Handle array wrapping.
        if (obj is object[] arr && arr.Length >= 2)
        {
            light = arr[1];
            obj = arr[0];
        }
        
        if (obj == null || IsNil(obj))
            return "nil".Eval();
        
        var gameObj = obj as Object;
        if (gameObj == null)
        {
            return "nil".Eval();
        }
        
        int lightValue = ToInt(light, 0);
        gameObj.Light = lightValue;
        
        return "nil".Eval();
    }
    
    /// <summary>
    /// (kern-obj-get-activity obj) -> string
    /// Returns the current activity as a string: "idle", "working", "sleeping", etc.
    /// </summary>
    public static object ObjectGetActivity(object objArg)
    {
        // Handle variadic array wrapper from IronScheme.
        if (objArg is object[] arr && arr.Length > 0)
            objArg = arr[0];
        
        Activity activity = Activity.Idle;
        
        if (objArg is Character ch)
            activity = ch.CurrentActivity;
        else if (objArg is Being being)
            activity = being.CurrentActivity;
        
        // Return string name, not integer - Scheme uses string=? to compare
        return activity switch
        {
            Activity.Idle => "idle",
            Activity.Working => "working",
            Activity.Sleeping => "sleeping",
            Activity.Commuting => "commuting",
            Activity.Eating => "eating",
            Activity.Drunk => "drunk",
            _ => "idle"
        };
    }

    /// <summary>
    /// (kern-obj-set-activity obj activity-string)
    /// Sets the current activity from a string name.
    /// </summary>
    public static object ObjectSetActivity(object objArg, object activityArg)
    {
        // Handle variadic array wrapper from IronScheme.
        if (objArg is object[] arr && arr.Length >= 2)
        {
            activityArg = arr[1];
            objArg = arr[0];
        }
        
        string name = activityArg?.ToString()?.ToLower() ?? "idle";
        
        Activity activity = name switch
        {
            "working" => Activity.Working,
            "sleeping" => Activity.Sleeping,
            "commuting" => Activity.Commuting,
            "eating" => Activity.Eating,
            "drunk" => Activity.Drunk,
            _ => Activity.Idle
        };
        
        if (objArg is Character ch)
            ch.CurrentActivity = activity;
        else if (objArg is Being being)
            being.CurrentActivity = activity;
        
        return "idle";
    }
    
    /// <summary>
    /// (kern-obj-heal object amount)
    /// Heals a being by the specified amount.
    /// </summary>
    public static object ObjectHeal(object[] args)
    {
        if (args == null || args.Length < 2)
        {
            Console.WriteLine($"[kern-obj-heal] Expected 2 args, got {args?.Length ?? 0}");
            return "nil".Eval();
        }
        
        object obj = args[0];
        int amount = Convert.ToInt32(args[1]);
        
        if (obj is Character character)
        {
            character.HP = Math.Min(character.HP + amount, character.MaxHP);
            Console.WriteLine($"{character.GetName()} heals {amount} HP (now {character.HP}/{character.MaxHP})");
        }
        else if (obj is Being being)
        {
            // Generic being heal.
            Console.WriteLine($"[kern-obj-heal] Being heals {amount}");
        }
        
        // Try tag resolution.
        if (obj is string)
        {
            string tag = ToTag(obj);
            if (!string.IsNullOrEmpty(tag))
            {
                var resolved = Phantasma.GetRegisteredObject(tag);
                if (resolved is Being resolvedBeing)
                {
                    resolvedBeing.Heal(amount);
                    return "nil".Eval();
                }
            }
        }
        
        Console.WriteLine($"[ERROR] kern-obj-heal: not a being (got {obj?.GetType().Name ?? "null"})");
        return "nil".Eval();
    }
    
    /// <summary>
    /// (kern-obj-is-char? obj)
    /// Returns #t if the object is a Character/Being.
    /// </summary>
    public static object ObjectIsChar(object objArg)
    {
        // Handle variadic array wrapper from IronScheme.
        if (objArg is object[] arr && arr.Length > 0)
            objArg = arr[0];
        
        // Same check as ObjectIsBeing — it's a being-layer check.
        return objArg is Being;
    }
}
