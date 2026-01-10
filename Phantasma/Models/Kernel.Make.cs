using System;
using System.Collections.Generic;
using System.Linq;
using IronScheme;
using IronScheme.Runtime;

namespace Phantasma.Models;

public partial class Kernel
{
    /// <summary>
    /// (kern-mk-sprite tag filename transparent-color)
    /// Creates a sprite from an image file.
    /// </summary>
    public static object MakeSprite(object tag, object spriteSet, object nFrames, object index, object wave, object facings)
    {
        string tagStr = tag?.ToString()?.TrimStart('\'');
        
        string ssTag = spriteSet?.ToString()?.Trim('"');
        dynamic ss = Phantasma.GetRegisteredObject(ssTag);
        
        // Load the sprite sheet image.
        string filename = ss.Filename?.ToString();
        var sourceImage = SpriteManager.LoadImage(filename); 
        
        // Calculate source coordinates from index.
        int idx = Convert.ToInt32(index ?? 0);
        int cols = ss.Cols;
        int tileWidth = ss.Width;
        int tileHeight = ss.Height;
        
        // Calculate grid position.
        int col = idx % cols;
        int row = idx / cols;
        
        var sprite = new Sprite
        {
            Tag = tagStr?.ToString(),  // The image filename from sprite set
            NFrames = Convert.ToInt32(nFrames ?? 1),
            //NTotalFrames
            //Facing
            //Facings
            //Sequence
            //Decor
            WPix = tileWidth,
            HPix = tileHeight,
            //Faded
            //Wave
            SourceImage = sourceImage,
            SourceX = col * tileWidth + ss.OffsetX,  // Calculate X position in sprite sheet
            SourceY = row * tileHeight + ss.OffsetY // Calculate Y position in sprite sheet
            //DisplayChar
        };
            
        // Register with Phantasma for lookup.
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, sprite);
            $"(define {tagStr} \"{tagStr}\")".Eval();
        }
        
        return sprite;
    }
    
    public static object MakeSpriteSet(object tag, object width, object height, object rows, object cols, object offx, object offy, object filename) 
    {
        string tagStr = tag?.ToString()?.TrimStart('\'');
    
        // Store the metadata in an anonymous object.
        var spriteSetData = new 
        {
            Tag = tagStr,
            Width = Convert.ToInt32(width ?? 32),
            Height = Convert.ToInt32(height ?? 32),
            Rows = Convert.ToInt32(rows ?? 1),
            Cols = Convert.ToInt32(cols ?? 1),
            OffsetX = Convert.ToInt32(offx ?? 0),
            OffsetY = Convert.ToInt32(offy ?? 0),
            Filename = filename?.ToString()
        };
    
        // Register with Phantasma for lookup.
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, spriteSetData);
            $"(define {tagStr} \"{tagStr}\")".Eval();
        }
    
        // Return the metadata. MakeSprite will use this.
        return spriteSetData;
    }
    
    /// <summary>
    /// (kern-mk-terrain tag name pclass sprite alpha light [effect-proc])
    /// Creates a terrain type.
    /// 
    /// Parameters:
    /// - tag: Symbol or string identifier (e.g., 't_grass)
    /// - name: Display name for the terrain
    /// - pclass: Passability class integer (see PassabilityTable constants)
    /// - sprite: Sprite object for rendering
    /// - alpha: Alpha transparency (0-255, 255 = fully opaque)
    /// - light: Light level emitted by terrain (0 = no light)
    /// - effect-proc: Optional procedure called when stepping on terrain
    /// </summary>
    public static object MakeTerrain(object[] args)
    {
        if (args.Length < 6) { /* error */ }
        
        int i = 0;
        
        // Required Parameters (0-5)
        string tagStr = ToTag(args[i++]);
        string name = args[i++]?.ToString()?.Trim('"') ?? tagStr;
        
        // DEBUG: Check what pclass actually is
        var pclassArg = args[i];
        Console.WriteLine($"[DEBUG MakeTerrain] {tagStr}: pclass arg type={pclassArg?.GetType().Name}, value={pclassArg}");
        
        int pclass = ToInt(args[i++], 0);
        Console.WriteLine($"[DEBUG MakeTerrain] {tagStr}: pclass result={pclass}");
        
        object spriteArg = args[i++];
        int alpha = ToInt(args[i++], 255);
        
        // Last required - only increment if there's an optional param following.
        int light = i < args.Length - 1 ? ToInt(args[i++], 0) : ToInt(args[i], 0);
        
        // Optional Parameter
        object effectProc = i < args.Length ? args[i] : null;
        
        // DEBUG: Trace sprite resolution.
        //Console.WriteLine($"[DEBUG MakeTerrain] {tagStr}: spriteArg type={spriteArg?.GetType().Name}, value={spriteArg}");
        
        // Resolve sprite after extraction.
        var sprite = spriteArg as Sprite ?? ResolveObject<Sprite>(spriteArg);
        
        // DEBUG: Check resolution result.
        //Console.WriteLine($"[DEBUG MakeTerrain] {tagStr}: sprite={sprite != null}, SourceImage={sprite?.SourceImage != null}");
        
        Terrain terrain = new Terrain(tagStr, name, sprite, pclass, alpha, light);
        Phantasma.RegisterObject(tagStr, terrain);
        $"(define {tagStr} \"{tagStr}\")".Eval();

        return terrain;
    }
    
    /// <summary>
    /// (kern-mk-terrain-type tag name pclass sprite)
    /// Creates a terrain type definition.
    /// </summary>
    public static object MakeTerrainType(object tag, object name, object pclass, object sprite)
    {
        string tagStr = tag?.ToString() ?? "unknown";
        string nameStr = name?.ToString() ?? tagStr;
        string pclassStr = pclass?.ToString() ?? ".g";
    
        // Parse passability from second char of pclass string.
        int passability = PassabilityTable.PCLASS_NONE;
        if (pclassStr.Length >= 2)
        {
            char passChar = pclassStr[1];
            passability = passChar switch
            {
                'g' => 1,  // Grass - passable
                '.' => PassabilityTable.PCLASS_NONE,  // Impassable
                'w' => 6,  // Water
                'm' => 8,  // Mountain
                _ => PassabilityTable.PCLASS_NONE
            };
        }
    
        var terrain = new Terrain
        {
            Name = nameStr,
            PassabilityClass = passability,
            Sprite = sprite as Sprite
        };
        
        return terrain;
    }
    
    /// <summary>
    /// (kern-mk-palette tag ((glyph1 terrain1) (glyph2 terrain2) ...))
    /// Creates a terrain palette for decoding map glyphs.
    /// </summary>
    public static object MakePalette(object tag, object mappings)
    {
        string tagStr = tag?.ToString()?.TrimStart('\'') ?? "unknown";
        
        var palette = new TerrainPalette(tagStr);
        
        // Convert Scheme list to vector for easier iteration.
        var mappingsVector = Builtins.ListToVector(mappings);
        int count = 0;
        
        if (mappingsVector is object[] mappingsArray)
        {
            foreach (var pairObj in mappingsArray)
            {
                if (pairObj == null) continue;
                
                // Each element is a (glyph terrain) pair.
                var glyph = Builtins.Car(pairObj);
                var rest = Builtins.Cdr(pairObj);
                var terrainObj = Builtins.Car(rest);
                
                string glyphStr = glyph?.ToString()?.Trim('"', '\'') ?? "";
                
                // Resolve terrain - could be Terrain object or tag string.
                Terrain terrain = null;
                if (terrainObj is Terrain t)
                {
                    terrain = t;
                }
                else if (terrainObj != null)
                {
                    // Try to resolve from tag (case-insensitive via GetRegisteredObject).
                    string terrainTag = terrainObj.ToString()?.TrimStart('\'').Trim('"');
                    terrain = Phantasma.GetRegisteredObject(terrainTag) as Terrain;
                }
            
                if (terrain != null && !string.IsNullOrEmpty(glyphStr))
                {
                    palette.AddMapping(glyphStr, terrain);
                    count++;
                }
            }
        }
    
        // Register palette for later use.
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, palette);
        
            // Define in Scheme so scripts can reference it.
            $"(define {tagStr} \"{tagStr}\")".Eval();
        }
        
        //Console.WriteLine($"  Created palette: {tagStr} ({count} entries.)");
        
        return palette;
    }
    
    /// <summary>
    /// (kern-mk-map tag width height palette (line1 line2 line3 ...))
    /// Creates a terrain map from glyph strings using a palette.
    /// </summary>
    public static object MakeMap(object tag, object width, object height, 
                                 object palette, object lines)
    {
        string? tagStr = tag?.ToString()?.TrimStart('\'');
        if (tagStr == "nil") tagStr = null;
        
        int w = Convert.ToInt32(width);
        int h = Convert.ToInt32(height);
        
        // Resolve palette - could be TerrainPalette object or tag string.
        TerrainPalette? pal = palette as TerrainPalette;
        if (pal == null && palette != null)
        {
            string palTag = palette.ToString()?.TrimStart('\'').Trim('"') ?? "";
            pal = Phantasma.GetRegisteredObject(palTag) as TerrainPalette;
        }
        
        if (pal == null)
        {
            Console.WriteLine($"[kern-mk-map] {tagStr}: could not resolve palette '{palette}'");
            return "nil".Eval();
        }
        
        var map = new TerrainMap(tagStr, w, h);
        
        // Convert Scheme list of lines to vector for easier iteration.
        var linesVector = Builtins.ListToVector(lines);
        
        if (linesVector is object[] linesArray)
        {
            for (int y = 0; y < linesArray.Length && y < h; y++)
            {
                var lineObj = linesArray[y];
                if (lineObj == null) continue;
                
                string line = lineObj.ToString()?.Trim('"') ?? "";
                
                // Split line by spaces to get individual glyphs.
                string[] glyphs = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                
                for (int x = 0; x < glyphs.Length && x < w; x++)
                {
                    string glyph = glyphs[x];
                    var terrain = pal.GetTerrainForGlyph(glyph);
                
                    if (terrain != null)
                    {
                        map.SetTerrain(x, y, terrain);
                    }
                }
            }
        }
        
        // Register map if it has a tag.
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, map);
            $"(define {tagStr} \"{tagStr}\")".Eval();
        }
        
        //Console.WriteLine($"  Created map: {tagStr} ({w}x{h})");
        
        return map;
    }
    
    /// <summary>
    /// (kern-mk-place tag name sprite map wraps underground wild combat
    ///                subplaces neighbors contents hooks entrances)
    /// Creates a place (map/location).
    /// </summary>
    public static object MakePlace(object[] args)
    {
        Console.WriteLine($"[DEBUG kern-mk-place] ENTRY - args count={args?.Length ?? 0}");
        
        if (args == null || args.Length < 8)
        {
            LoadError($"kern-mk-place: expected at least 8 args, got {args?.Length ?? 0}");
            return "nil".Eval();
        }
        
        int i = 0;
        
        // Required Parameters (0-7)
        string tagStr = ToTag(args[i++]);
        string nameStr = args[i++]?.ToString()?.Trim('"') ?? "Unnamed Place";
        object spriteArg = args[i++];
        object mapArg = args[i++];
        bool wraps = ConvertToBool(args[i++]);
        bool underground = ConvertToBool(args[i++]);
        bool wild = ConvertToBool(args[i++]);
        
        // Last required - only increment if there are optional params following.
        bool combat = i < args.Length - 1 ? ConvertToBool(args[i++]) : ConvertToBool(args[i]);
        
        // Optional parameters (8-12) - use MakeCharacter pattern
        object subplacesArg = i < args.Length - 1 ? args[i++] : null;
        object neighborsArg = i < args.Length - 1 ? args[i++] : null;
        object contentsArg = i < args.Length - 1 ? args[i++] : null;
        object hooksArg = i < args.Length - 1 ? args[i++] : null;
        object entrancesArg = i < args.Length ? args[i] : null;
        
        Console.WriteLine($"  Creating place: {tagStr} - {nameStr}");
        
        // Resolve map.
        TerrainMap terrainMap = default;
        if (mapArg is TerrainMap tm)
            terrainMap = tm;
        else if (mapArg != null && !IsNil(mapArg))
        {
            var resolved = Phantasma.GetRegisteredObject(ToTag(mapArg));
            if (resolved is TerrainMap resolvedMap)
                terrainMap = resolvedMap;
        }
        
        // Resolve sprite.
        Sprite sprite = null;
        if (spriteArg is Sprite s)
            sprite = s;
        else if (spriteArg != null && !IsNil(spriteArg))
        {
            var resolved = Phantasma.GetRegisteredObject(ToTag(spriteArg));
            if (resolved is Sprite resolvedSprite)
                sprite = resolvedSprite;
        }
        
        var place = new Place
        {
            Tag = tagStr,
            Name = nameStr,
            Sprite = sprite,
            Width = terrainMap.Width > 0 ? terrainMap.Width : 32,
            Height = terrainMap.Height > 0 ? terrainMap.Height : 32,
            TerrainGrid = terrainMap.TerrainGrid,
            Wraps = wraps,
            Underground = underground,
            Wilderness = wild,
            CombatEnabled = combat
        };
        
        place.SetDefaultEdgeEntrances();
        
        Console.WriteLine($"    Place created ({place.Width}x{place.Height})");
        
        // Load subplaces.
        if (subplacesArg != null && !IsNil(subplacesArg))
        {
            Cons list = subplacesArg as Cons;
            while (list != null)
            {
                if (list.car is Cons entry)
                {
                    Place subplace = null;
                    var subplaceRef = entry.car;
                    
                    if (subplaceRef is Place sp)
                        subplace = sp;
                    else if (subplaceRef != null && !IsNil(subplaceRef))
                    {
                        var resolved = Phantasma.GetRegisteredObject(ToTag(subplaceRef));
                        if (resolved is Place resolvedPlace)
                            subplace = resolvedPlace;
                    }
                    
                    if (subplace != null)
                    {
                        var rest = entry.cdr as Cons;
                        if (rest != null)
                        {
                            int x = ToInt(rest.car, 0);
                            int y = rest.cdr is Cons rest2 ? ToInt(rest2.car, 0) : 0;
                            
                            if (place.AddSubplace(subplace, x, y))
                                Console.WriteLine($"    Added subplace {subplace.Tag} at ({x}, {y})");
                        }
                    }
                }
                list = list.cdr as Cons;
            }
        }
        
        // Load neighbors.
        if (neighborsArg != null && !IsNil(neighborsArg))
        {
            Cons list = neighborsArg as Cons;
            while (list != null)
            {
                if (list.car is Cons entry)
                {
                    Place neighbor = null;
                    var neighborRef = entry.car;
                    
                    if (neighborRef is Place np)
                        neighbor = np;
                    else if (neighborRef != null && !IsNil(neighborRef))
                    {
                        var resolved = Phantasma.GetRegisteredObject(ToTag(neighborRef));
                        if (resolved is Place resolvedPlace)
                            neighbor = resolvedPlace;
                    }
                    
                    if (neighbor != null)
                    {
                        var rest = entry.cdr as Cons;
                        if (rest != null)
                        {
                            int dir = ToInt(rest.car, -1);
                            
                            if (dir == Common.UP)
                            {
                                place.Above = neighbor;
                                neighbor.Below = place;
                                Console.WriteLine($"    Linked {neighbor.Tag} above");
                            }
                            else if (dir == Common.DOWN)
                            {
                                place.Below = neighbor;
                                neighbor.Above = place;
                                Console.WriteLine($"    Linked {neighbor.Tag} below");
                            }
                        }
                    }
                }
                list = list.cdr as Cons;
            }
        }
        
        // Load contents: list of (obj x y)
        if (contentsArg != null && !IsNil(contentsArg))
        {
            Cons list = contentsArg as Cons;
            while (list != null)
            {
                if (list.car is Cons entry)
                {
                    var objRef = entry.car;
                    var rest = entry.cdr as Cons;
                    
                    if (objRef != null && !IsNil(objRef) && objRef != "nil".Eval() && rest != null)
                    {
                        int x = ToInt(rest.car, 0);
                        int y = rest.cdr is Cons rest2 ? ToInt(rest2.car, 0) : 0;
                        
                        Object gameObj = null;
                        if (objRef is Object directObj)
                            gameObj = directObj;
                        else
                        {
                            var resolved = Phantasma.GetRegisteredObject(ToTag(objRef));
                            if (resolved is Object resolvedObj)
                                gameObj = resolvedObj;
                        }
                        
                        if (gameObj != null)
                        {
                            place.AddObject(gameObj, x, y);
                        }
                    }
                }
                list = list.cdr as Cons;
            }
        }
        
        // Load hooks.
        if (hooksArg != null && !IsNil(hooksArg))
        {
            if (hooksArg is Callable callable)
                place.PreEntryHook = callable;
            else if (hooksArg is Cons hookList && hookList.car is Callable hookCallable)
                place.PreEntryHook = hookCallable;
        }
        
        // Load entrances.
        if (entrancesArg != null && !IsNil(entrancesArg))
        {
            Cons list = entrancesArg as Cons;
            while (list != null)
            {
                if (list.car is Cons entry)
                {
                    int dir = ToInt(entry.car, -1);
                    var rest = entry.cdr as Cons;
                    
                    if (rest != null && dir >= 0)
                    {
                        int x = ToInt(rest.car, 0);
                        int y = rest.cdr is Cons rest2 ? ToInt(rest2.car, 0) : 0;
                        
                        if (place.SetEdgeEntrance((Direction)dir, x, y))
                            Console.WriteLine($"    Set entrance for {Common.DirectionToString(dir)} at ({x}, {y})");
                    }
                }
                list = list.cdr as Cons;
            }
        }
        
        // Register the place.
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, place);
            $"(define {tagStr} \"{tagStr}\")".Eval();
        }
        
        Console.WriteLine($"  Created place: {tagStr} ({place.Width}x{place.Height})");
        
        return place;
    }
    
    /// <summary>
    /// (kern-mk-mmode tag name index)
    /// Creates a movement mode.
    /// </summary>
    public static object MakeMovementMode(object[] args)
    {
        if (args.Length < 3) { /* error */ }
        string tagStr = ToTag(args[0]);
        string nameStr = args[1]?.ToString()?.Trim('"') ?? "Unknown";
        int index = ToInt(args[2], 0);
    
        var mmode = new MovementMode(tagStr, nameStr, index);
        Phantasma.RegisterObject(tagStr, mmode);
        $"(define {tagStr} \"{tagStr}\")".Eval();
        
        //Console.WriteLine($"  Created mmode: {nameStr} (index={index})");
        return mmode;
    }
    
    /// <summary>
    /// (kern-mk-species tag name str int dex spd vr mmode 
    ///                  hpmod hpmult mpmod mpmult
    ///                  sleep-sprite weapon visible 
    ///                  damage-sound walking-sound on-death
    ///                  xpval slots spells)
    /// Creates a species definition - full 21-parameter Nazghul signature.
    /// </summary>
    public static object MakeSpecies(object[] args)
    {
        if (args.Length < 21)
        {
            LoadError($"kern-mk-species: expected 21 args, got {args.Length}");
            return "nil".Eval();
        }
        
        // Extract parameters.
        string tagStr = ToTag(args[0]);                                    // 0: y = tag
        string nameStr = args[1]?.ToString()?.Trim('"') ?? "Unknown";      // 1: s = name
        int str = ToInt(args[2], 10);                                      // 2: d = str
        int intl = ToInt(args[3], 10);                                     // 3: d = intl
        int dex = ToInt(args[4], 10);                                      // 4: d = dex
        int spd = ToInt(args[5], 10);                                      // 5: d = spd
        int vr = ToInt(args[6], 10);                                       // 6: d = vr (vision radius)
        object mmodeArg = args[7];                                         // 7: p = mmode
        int hpmod = ToInt(args[8], 10);                                    // 8: d = hpmod
        int hpmult = ToInt(args[9], 5);                                    // 9: d = hpmult
        int mpmod = ToInt(args[10], 5);                                    // 10: d = mpmod
        int mpmult = ToInt(args[11], 2);                                   // 11: d = mpmult
        object sleepSpriteArg = args[12];                                  // 12: p = sleep_sprite
        object weaponArg = args[13];                                       // 13: p = weapon
        bool visible = ConvertToBool(args[14] ?? true);                    // 14: b = visible
        object damageSoundArg = args[15];                                  // 15: p = damage_sound
        object walkingSoundArg = args[16];                                 // 16: p = walking_sound
        object onDeathArg = args[17];                                      // 17: c = on_death
        int xpval = ToInt(args[18], 10);                                   // 18: d = xpval
        object slotsArg = args[19];                                        // 19: slots list
        object spellsArg = args[20];                                       // 20: spells list

        // Resolve movement mode (may be a struct - use same pattern as existing code).
        MovementMode movementMode;
        if (mmodeArg is MovementMode mm)
        {
            movementMode = mm;
        }
        else if (mmodeArg is int i)
        {
            movementMode = new MovementMode(null, "Walking", i);
        }
        else
        {
            // Try to resolve by tag.
            var resolved = Phantasma.GetRegisteredObject(ToTag(mmodeArg));
            if (resolved is MovementMode rmm)
                movementMode = rmm;
            else
                movementMode = new MovementMode("mmode-walk", "Walking", 0);
        }
        
        // Parse slots list into int array.
        int[]? slotsArray = ParseSlotsList(slotsArg);
        
        // Parse spells list into string array.
        string[]? spellsArray = ParseSpellsList(spellsArg);
        
        // Create species struct.
        var species = new Species
        {
            Tag = tagStr,
            Name = nameStr,
            Str = str,
            Intl = intl,
            Dex = dex,
            Spd = spd,
            Vr = vr,
            MovementMode = movementMode,
            HpMod = hpmod,
            HpMult = hpmult,
            MpMod = mpmod,
            MpMult = mpmult,
            SleepSprite = ResolveObject<Sprite>(sleepSpriteArg),
            Weapon = ResolveObject<ArmsType>(weaponArg),
            Visible = visible,
            XpVal = xpval,
            Slots = slotsArray,
            Spells = spellsArray
        };
        
        // Set on-death closure if provided.
        if (!IsNil(onDeathArg))
        {
            species.OnDeath = onDeathArg;
        }
        
        // Set sounds if provided.
        species.DamageSound = ResolveObject<Sound>(damageSoundArg);
        species.MovementSound = ResolveObject<Sound>(walkingSoundArg);
        
        // Register the species.
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, species);
            $"(define {tagStr} \"{tagStr}\")".Eval();
        }
        
        //Console.WriteLine($"  Created species: {nameStr} (str={str}, dex={dex}, hp={hpmod}+{hpmult}/lvl)");
        
        return species;
    }
    
    /// <summary>
    /// (kern-mk-occ tag name magic hpmod hpmult mpmod mpmult hit def dam arm xpval)
    /// Creates an occupation.
    /// </summary>
    public static object MakeOccupation(
        object tag, object name, object magic,
        object hpmod, object hpmult, object mpmod, object mpmult,
        object hit, object def, object dam, object arm,
        object xpval)
    {
        string tagStr = tag?.ToString()?.TrimStart('\'');
        
        var occ = new Occupation
        {
            Tag = tagStr,
            Name = name?.ToString() ?? "Unknown",
            Magic = Convert.ToSingle(magic ?? 1.0f),
            HpMod = Convert.ToInt32(hpmod ?? 0),
            HpMult = Convert.ToInt32(hpmult ?? 0),
            MpMod = Convert.ToInt32(mpmod ?? 0),
            MpMult = Convert.ToInt32(mpmult ?? 0),
            HitMod = Convert.ToInt32(hit ?? 0),
            DefMod = Convert.ToInt32(def ?? 0),
            DamMod = Convert.ToInt32(dam ?? 0),
            ArmMod = Convert.ToInt32(arm ?? 0),
            XpVal = Convert.ToInt32(xpval ?? 0)
        };
        
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, occ);
            $"(define {tagStr} \"{tagStr}\")".Eval();
        }
        
        //Console.WriteLine($"  Created occupation: {occ.Name} (magic={occ.Magic:F1}, hp+{occ.HpMod}+{occ.HpMult}/lvl)");
        
        return occ;
    }
    
    /// <summary>
    /// (kern-mk-char tag name species occ sprite base-faction
    ///              str int dex hpmod hpmult mpmod mpmult
    ///              hp xp mp lvl dead
    ///              conv sched ai inventory
    ///              readied-list hooks-list)
    /// Creates a character - full 24-parameter Nazghul signature.
    /// </summary>
    public static object MakeCharacter(object[] args)
    {
        if (args == null)
        {
            LoadError("kern-mk-char: args is null");
            return null;
        }
        
        if (args.Length < 21)
        {
            LoadError($"kern-mk-char: expected at least 21 args, got {args.Length}");
            return null;
        }
        
        int i = 0;
        
        // Required Parameters (0-17)
        string tagStr = ToTag(args[i++]);
        string nameStr = args[i++]?.ToString()?.Trim('"') ?? "Unknown";
        object speciesArg = args[i++];
        object occArg = args[i++];
        object spriteArg = args[i++];
        int baseFaction = ToInt(args[i++], 0);
        int str = ToInt(args[i++], 10);
        int intl = ToInt(args[i++], 10);
        int dex = ToInt(args[i++], 10);
        int hpmod = ToInt(args[i++], 0);
        int hpmult = ToInt(args[i++], 0);
        int mpmod = ToInt(args[i++], 0);
        int mpmult = ToInt(args[i++], 0);
        int hp = ToInt(args[i++], 0);
        int xp = ToInt(args[i++], 0);
        int mp = ToInt(args[i++], 0);
        int lvl = ToInt(args[i++], 1);
        
        // Last required - only increment if there are optional params following.
        bool dead = i < args.Length - 1 ? ConvertToBool(args[i++]) : ConvertToBool(args[i]);
        
        // Optional parameters - only increment if there's another element after.
        object convArg = i < args.Length - 1 ? args[i++] : null;
        object schedArg = i < args.Length - 1 ? args[i++] : null;
        object aiArg = i < args.Length - 1 ? args[i++] : null;
        object inventoryArg = i < args.Length - 1 ? args[i++] : null;
        object readiedArg = i < args.Length - 1 ? args[i++] : null;
        object hooksArg = i < args.Length ? args[i] : null;  // Last one - just check existence.
        
        // Resolve sprite.
        Sprite sprite = null;
        if (spriteArg is Sprite s)
            sprite = s;
        else if (spriteArg != null && !IsNil(spriteArg))
        {
            var resolved = Phantasma.GetRegisteredObject(ToTag(spriteArg));
            if (resolved is Sprite resolvedSprite)
                sprite = resolvedSprite;
        }
        
        // Create character.
        var character = new Character();
        character.SetName(nameStr);
        
        if (sprite != null)
            character.CurrentSprite = sprite;
        
        // Set species (struct).
        if (speciesArg is Species sp)
        {
            character.Species = sp;
        }
        else if (speciesArg != null && !IsNil(speciesArg))
        {
            var resolved = Phantasma.GetRegisteredObject(ToTag(speciesArg));
            if (resolved is Species resolvedSpecies)
                character.Species = resolvedSpecies;
        }
        
        // Set occupation (struct).
        if (occArg is Occupation o)
        {
            character.Occupation = o;
        }
        else if (occArg != null && !IsNil(occArg))
        {
            var resolved = Phantasma.GetRegisteredObject(ToTag(occArg));
            if (resolved is Occupation resolvedOcc)
                character.Occupation = resolvedOcc;
        }
        
        // Set base faction and stats.
        character.SetBaseFaction(baseFaction);
        character.Strength = str;
        character.Intelligence = intl;
        character.Dexterity = dex;
        character.HpMod = hpmod;
        character.HpMult = hpmult;
        character.MpMod = mpmod;
        character.MpMult = mpmult;
        character.Level = lvl;
        
        // Calculate HP and MP.
        character.MaxHP = character.GetMaxHp();
        character.HP = hp > 0 ? hp : character.MaxHP;
        character.MaxMP = character.GetMaxMana();
        character.MP = mp > 0 ? mp : character.MaxMP;
        character.Experience = xp;
        
        if (dead || hp == 0)
            character.HP = 0;
        
        // Store conversation closure.
        if (convArg != null && !IsNil(convArg))
            character.Conversation = convArg;
        
        // Store AI closure.
        if (aiArg != null && !IsNil(aiArg))
            character.AIBehavior = aiArg;
        
        // Set schedule.
        if (schedArg != null && !IsNil(schedArg))
        {
            if (schedArg is Schedule sched)
                character.Schedule = sched;
            else
            {
                var resolved = Phantasma.GetRegisteredObject(ToTag(schedArg));
                if (resolved is Schedule resolvedSched)
                    character.Schedule = resolvedSched;
            }
        }
        
        // Set inventory.
        if (inventoryArg != null && !IsNil(inventoryArg))
        {
            if (inventoryArg is Container cont)
                character.SetInventoryContainer(cont);
            else
            {
                var resolved = Phantasma.GetRegisteredObject(ToTag(inventoryArg));
                if (resolved is Container resolvedCont)
                    character.SetInventoryContainer(resolvedCont);
            }
        }
        
        // Process readied arms list - SAFELY.
        if (readiedArg != null && !IsNil(readiedArg))
        {
            Cons currentReadied = readiedArg as Cons;
            while (currentReadied != null)
            {
                var item = currentReadied.car;
                
                // Skip null, nil, or unspecified items.
                if (item == null || IsNil(item) || item == null)
                {
                    currentReadied = currentReadied.cdr as Cons;
                    continue;
                }
                
                ArmsType arms = null;
                
                if (item is ArmsType at)
                {
                    arms = at;
                }
                else
                {
                    string armTag = ToTag(item);
                    if (!string.IsNullOrEmpty(armTag))
                    {
                        var resolved = Phantasma.GetRegisteredObject(armTag);
                        if (resolved is ArmsType resolvedArms)
                            arms = resolvedArms;
                        else
                            Console.WriteLine($"  [WARNING] kern-mk-char {tagStr}: readied item '{armTag}' not found or not ArmsType");
                    }
                }
                
                if (arms != null)
                {
                    character.Ready(arms);
                }
                
                currentReadied = currentReadied.cdr as Cons;
            }
        }
        
        // Process hooks list - SAFELY.
        if (hooksArg != null && !IsNil(hooksArg))
        {
            Cons currentHook = hooksArg as Cons;
            while (currentHook != null)
            {
                var hookItem = currentHook.car;
                
                if (hookItem == null || IsNil(hookItem) || hookItem == null)
                {
                    currentHook = currentHook.cdr as Cons;
                    continue;
                }
                
                if (hookItem is Cons hookEntry)
                {
                    var effectArg = hookEntry.car;
                    Effect effect = null;
                    
                    if (effectArg is Effect e)
                        effect = e;
                    else if (effectArg != null && !IsNil(effectArg))
                    {
                        var resolved = Phantasma.GetRegisteredObject(ToTag(effectArg));
                        if (resolved is Effect resolvedEffect)
                            effect = resolvedEffect;
                    }
                    
                    if (effect != null)
                    {
                        object gob = null;
                        int duration = effect.Duration;
                        int flags = 0;
                        
                        var rest = hookEntry.cdr as Cons;
                        if (rest != null)
                        {
                            gob = rest.car;
                            if (gob != null && IsNil(gob)) 
                                gob = null;
                            
                            var rest2 = rest.cdr as Cons;
                            if (rest2 != null)
                            {
                                duration = ToInt(rest2.car, effect.Duration);
                                
                                var rest3 = rest2.cdr as Cons;
                                if (rest3 != null)
                                    flags = ToInt(rest3.car, 0);
                            }
                        }
                        
                        character.RestoreEffect(effect, gob, flags, duration);
                    }
                }
                
                currentHook = currentHook.cdr as Cons;
            }
        }
        
        // Register the character.
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, character);
            $"(define {tagStr} \"{tagStr}\")".Eval();
        }
        
        Console.WriteLine($"  Created character: {nameStr} (str={character.GetStrength()}, " +
                          $"hp={character.HP}/{character.MaxHP}, lvl={lvl})");
        
        return character;
    }
    
    /// <summary>
    /// (kern-mk-obj type count)
    /// Creates an object instance from a type.
    /// </summary>
    public static object MakeObject(object type, object count)
    {
        int itemCount = ToInt(count, 1);
        
        // Handle null or nil type.
        if (type == null || IsNil(type))
            return "nil".Eval();
        
        // Resolve the ObjectType.
        ObjectType objType = null;
    
        if (type is ObjectType ot)
            objType = ot;
        else if (type is ArmsType at)
            return CreateArmsItem(at, itemCount);  // Arms are always items.
        else if (type is FieldType ft)
            return new Field(ft, ft.Duration);
        else
        {
            string tagStr = ToTag(type);
            var resolved = Phantasma.GetRegisteredObject(tagStr);
            
            if (resolved is ObjectType rot)
                objType = rot;
            else if (resolved is ArmsType rat)
                return CreateArmsItem(rat, itemCount);
            else if (resolved is FieldType rft)
                return new Field(rft, rft.Duration);
            else
            {
                Console.WriteLine($"[kern-mk-obj] Unknown type: {tagStr}");
                return "nil".Eval();
            }
        }
    
        // Check the ObjectType's layer and create appropriate object.
        switch (objType.Layer)
        {
            case ObjectLayer.TerrainFeature:
                // Create a TerrainFeature object, not an Item!
                var tfeat = new TerrainFeature
                {
                    Name = objType.Name,
                    Type = objType,
                    // Inherit passability class from type (default to 1 for walkable).
                    PassabilityClass = 1
                };
                Console.WriteLine($"[kern-mk-obj] Created TerrainFeature: {objType.Name}");
                return tfeat;
            
            case ObjectLayer.Mechanism:
                // Create a mechanism object
                var mech = new Mechanism
                {
                    Name = objType.Name,
                    Type = objType
                };
                return mech;
            
            case ObjectLayer.Portal:
                var portal = new Portal
                {
                    Name = objType.Name,
                    Type = objType
                };
                return portal;
            
            default:
                // Default: create an Item
                var item = new Item
                {
                    Type = objType,
                    Count = itemCount
                };
                item.Name = objType.Name;
                return item;
        }
    }
    
    private static Item CreateArmsItem(ArmsType armsType, int count)
    {
        return new Item
        {
            Type = armsType,
            Count = count,
            Name = armsType.Name
        };
    }
    
    /// <summary>
    /// (kern-mk-obj-type tag name sprite layer gifc-cap gifc)
    /// Creates an object type - Nazghul-compatible 6-parameter signature.
    /// </summary>
    public static object MakeObjectType(
        object tag, object name, object sprite, object layer,
        object capabilities, object interactionHandler)
    {
        string tagStr = tag?.ToString()?.TrimStart('\'');
    
        var objType = new ObjectType
        {
            Tag = tagStr ?? "unknown",
            Name = name?.ToString() ?? "Unknown",
            Layer = (ObjectLayer)Convert.ToInt32(layer ?? 0),
            Capabilities = Convert.ToInt32(capabilities ?? 0)
        };
        
        if (sprite is Sprite s)
            objType.Sprite = s;
        else if (sprite != null)
        {
            // Try to resolve tag.
            var resolved = Phantasma.GetRegisteredObject(sprite.ToString().TrimStart('\'').Trim('"'));
            if (resolved is Sprite resolvedSprite)
                objType.Sprite = resolvedSprite;
        }
        
        // Store interaction handler closure for later use.
        if (interactionHandler != null && !(interactionHandler is bool b && b == false))
            objType.InteractionHandler = interactionHandler;
        
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, objType);
            $"(define {tagStr} \"{tagStr}\")".Eval();
        }
        
        //Console.WriteLine($"  Created object type: {tagStr} '{objType.Name}'");
        
        return objType;
    }

    /// <summary>
    /// (kern-mk-arms-type tag name sprite to-hit damage armor defend
    ///                    slots hands range rap missile thrown ubiq
    ///                    weight fire-sound gifc-cap gifc)
    /// Creates a weapon or armor type.
    /// </summary>
    public static object MakeArmsType(object[] args)
    {
        if (args.Length < 18)
        {
            LoadError($"kern-mk-arms-type: expected 18 args, got {args.Length}");
            return "nil".Eval();
        }
        
        // Extract parameters.
        string tagStr = ToTag(args[0]);                                    // 0: y = tag
        string nameStr = args[1]?.ToString()?.Trim('"') ?? "Unknown";      // 1: s = name
        object spriteArg = args[2];                                        // 2: p = sprite
        string toHitDice = args[3]?.ToString()?.Trim('"') ?? "0";          // 3: s = hit dice
        string damageDice = args[4]?.ToString()?.Trim('"') ?? "0";         // 4: s = damage dice
        string armorDice = args[5]?.ToString()?.Trim('"') ?? "0";          // 5: s = armor dice
        string defendDice = args[6]?.ToString()?.Trim('"') ?? "0";         // 6: s = defend dice
        int slots = ToInt(args[7], 0x01);                                  // 7: d = slots (bitmask)
        int hands = ToInt(args[8], 1);                                     // 8: d = hands required
        int range = ToInt(args[9], 1);                                     // 9: d = range
        int rap = ToInt(args[10], 1);                                      // 10: d = required action points
        object missileArg = args[11];                                      // 11: p = missile type
        bool thrown = ConvertToBool(args[12]);                             // 12: b = thrown
        bool ubiq = ConvertToBool(args[13]);                               // 13: b = ubiquitous ammo
        int weight = ToInt(args[14], 0);                                   // 14: d = weight
        object fireSoundArg = args[15];                                    // 15: p = fire_sound
        int gifcCap = ToInt(args[16], 0);                                  // 16: d = gifc_cap
        object gifcArg = args[17];                                         // 17: o = gifc closure
        
        // Validate dice notation.
        if (!Dice.IsValid(toHitDice))
        {
            LoadError($"kern-mk-arms-type {tagStr}: bad to-hit dice '{toHitDice}'");
            return "nil".Eval();
        }
        if (!Dice.IsValid(damageDice))
        {
            LoadError($"kern-mk-arms-type {tagStr}: bad damage dice '{damageDice}'");
            return "nil".Eval();
        }
        if (!Dice.IsValid(armorDice))
        {
            LoadError($"kern-mk-arms-type {tagStr}: bad armor dice '{armorDice}'");
            return "nil".Eval();
        }
        if (!Dice.IsValid(defendDice))
        {
            LoadError($"kern-mk-arms-type {tagStr}: bad defend dice '{defendDice}'");
            return "nil".Eval();
        }
        
        // Resolve missile type (for ranged weapons).
        var missileType = ResolveObject<ArmsType>(missileArg);
        
        // Use the ArmsType constructor.
        var armsType = new ArmsType(
            tag: tagStr,
            name: nameStr,
            sprite: ResolveObject<Sprite>(spriteArg),
            slotMask: slots,
            toHitDice: toHitDice,
            toDefendDice: defendDice,
            numHands: hands,
            range: range,
            weight: weight,
            damageDice: damageDice,
            armorDice: armorDice,
            requiredActionPoints: rap,
            thrown: thrown,
            ubiquitousAmmo: ubiq,
            missileType: missileType
        );
        
        // Set fire sound if provided.
        armsType.FireSound = ResolveObject<Sound>(fireSoundArg);
        
        // Store gifc closure if provided (for interaction handler)
        // Note: ArmsType inherits from ObjectType which has InteractionHandler
        // but we may need to check if this property exists.
        // if (!IsNil(gifcArg))
        //     armsType.InteractionHandler = gifcArg;
        
        // Register the arms type.
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, armsType);
            $"(define {tagStr} \"{tagStr}\")".Eval();
        }
        
        //Console.WriteLine($"  Created arms type: {nameStr} (dmg={damageDice}, rng={range})");
        
        return armsType;
    }
    
    /// <summary>
    /// (kern-mk-container type trap contents-list)
    /// Creates a container (inventory).
    /// </summary>
    public static object MakeContainer(object type, object trap, object contentsList)
    {
        var container = new Container();
    
        // Type can be nil for player inventory.
        if (type is ObjectType ot)
            container.Type = ot;
    
        // trap - TODO: Implement when closure system ready.
    
        // Contents - List of (Count Type) Pairs
        if (contentsList is Cons contents)
        {
            while (contents != null)
            {
                if (contents.car is Cons entry)
                {
                    int count = Convert.ToInt32(entry.car ?? 1);
                    var rest = entry.cdr as Cons;
                    if (rest?.car is ObjectType itemType)
                    {
                        container.AddItem(new Item(){ Type = itemType, Count = count});
                    }
                }
                contents = contents.cdr as Cons;
            }
        }
    
        //Console.WriteLine($"  Created container with {container.Capacity} item types");
    
        return container;
    }
    
    /// <summary>
    /// (kern-mk-party type faction vehicle)
    /// Creates a party.
    /// </summary>
    public static object MakeParty(object type, object faction, object vehicle)
    {        
        var party = new Party();
        // type - PartyType, ignored for now (TODO: implement PartyType)
        party.Faction = Convert.ToInt32(faction ?? 0);
        // vehicle - Vehicle the party is in, ignored for now
        party.IsPlayerParty = false;
    
        //Console.WriteLine($"  Created party (faction={party.Faction})");
    
        return party;
    }
    
    // <summary>
    /// (kern-mk-player tag sprite mv-desc mv-sound food gold ttnm 
    ///                 formation campsite camp-formation vehicle inventory
    ///                 (list members...))
    /// Creates the player party - full 13-parameter Nazghul signature.
    /// </summary>
    public static object MakePlayer(
        object tag, object sprite, object mvDesc, object mvSound,
        object food, object gold, object ttnm,
        object formation, object campsite, object campFormation,
        object vehicle, object inventory,
        object membersList)
    {
        string tagStr = tag?.ToString()?.TrimStart('\'');
        
        // Create the player party.
        var party = new Party();
        party.IsPlayerParty = true;
        party.Faction = 0; // Player faction
        
        // Set sprite if provided.
        if (sprite is Sprite s)
            party.Sprite = s;
        
        // Set movement description.
        party.MovementDescription = mvDesc?.ToString() ?? "walking";
        
        // Set resources
        party.Food = Convert.ToInt32(food ?? 0);
        party.Gold = Convert.ToInt32(gold ?? 0);
        party.TurnsToNextMeal = Convert.ToInt32(ttnm ?? 100);
        
        // TODO: formation, campsite, campFormation - Implement when needed.
        // TODO: mvSound - Implement when sound system ready.
        // TODO: vehicle - Implement when vehicle system ready.
        
        // Set inventory container.
        if (inventory is Container inv)
            party.Inventory = inv;
        
        // Add members from the list.
        Character firstMember = null;
        if (membersList is Cons members)
        {
            while (members != null)
            {
                if (members.car is Character ch)
                {
                    // Check position BEFORE adding to party.
                    var posBefore = ch.GetPosition();
                    Console.WriteLine($"  MakePlayer: {ch.GetName()} position BEFORE AddMember: Place={posBefore?.Place?.Name ?? "NULL"}, X={posBefore?.X}, Y={posBefore?.Y}");
    
                    party.AddMember(ch);
    
                    // Check position AFTER adding to party.
                    var posAfter = ch.GetPosition();
                    Console.WriteLine($"  MakePlayer: {ch.GetName()} position AFTER AddMember: Place={posAfter?.Place?.Name ?? "NULL"}, X={posAfter?.X}, Y={posAfter?.Y}");
    
                    if (firstMember == null)
                        firstMember = ch;
                }
                members = members.cdr as Cons;
            }
        }
        
        // Register the party.
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, party);
            $"(define {tagStr} \"{party.Tag}\")".Eval();
        }
        Phantasma.RegisterObject(KEY_PLAYER_PARTY, party);
        $"(define {KEY_PLAYER_PARTY} \"{party.Tag}\")".Eval();
        
        // Register the first member as the player character.
        if (firstMember != null)
        {
            Phantasma.RegisterObject(KEY_PLAYER_CHARACTER, firstMember);
            $"(define {KEY_PLAYER_CHARACTER} \"{firstMember}\")".Eval();
            Console.WriteLine($"  Set player character: {firstMember.GetName()}");
        }
        
        Phantasma.SetPendingPlayerParty(party);
        if (firstMember != null)
        {
            Phantasma.SetPendingPlayerCharacter(firstMember);
        }
        
        Console.WriteLine($"  Created player party with {party.Size} members (food={party.Food}, gold={party.Gold})");
        
        return party;
    }
    /// <summary>
    /// (kern-fire-missile missile-type origin-loc dest-loc)
    /// Fire a missile from origin to destination.
    /// </summary>
    public static object FireMissile(object missileTypeObj, object originLocObj, object destLocObj)
    {
        var missileType = missileTypeObj as ArmsType;
        var originLoc = originLocObj as Location;
        var destLoc = destLocObj as Location;
        
        // Fire the missile using the ArmsType's Fire method.
        bool hit = missileType.Fire(originLoc.Place,
            originLoc.X, originLoc.Y,
            destLoc.X, destLoc.Y);
        /*
        // Run hit-location procedure if it hit.
        if (hit && missileType.CanHitLocation)
        {
            var missile = missileType.GetMissile();
            if (missile != null)
            {
                missileType.HitLocation(missile, destLoc.Place, destLoc.X, destLoc.Y);
            }
        }
        */
        return hit ? "#t".Eval() : "#f".Eval();
    }
    
    /// <summary>
    /// (kern-mk-reagent-type tag name sprite char)
    /// Create a reagent type.
    /// </summary>
    public static object MakeReagentType(object tag, object name, object sprite, object displayChar)
    {
        string tagStr = tag?.ToString()?.TrimStart('\'') ?? "unknown";
        string nameStr = name?.ToString() ?? "Unnamed Reagent";
        char charVal = displayChar?.ToString().FirstOrDefault() ?? '?';
        
        var reagentType = new ReagentType
        {
            Tag = tagStr,
            Name = nameStr,
            DisplayChar = charVal,
            Sprite = sprite as Sprite
        };
        
        //Console.WriteLine($"  Created reagent type: {tagStr} - {nameStr}");
        
        // Register for later lookup.
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, reagentType);
        }
        
        return reagentType;
    }
    
    /// <summary>
    /// (kern-mk-spell tag name level mana-cost range sprite 
    ///               can-target-empty? can-target-ally? can-target-enemy? can-target-self?
    ///               requires-los? (reagent-list) effect-closure)
    /// Create a spell type.
    /// </summary>
    public static object MakeSpell(
        object tag, object name, object level, object manaCost, object range,
        object sprite, object canTargetEmpty, object canTargetAlly, 
        object canTargetEnemy, object canTargetSelf, object requiresLos,
        object reagents, object effect)
    {
        string tagStr = tag?.ToString()?.TrimStart('\'') ?? "unknown";
        string nameStr = name?.ToString() ?? "Unnamed Spell";
        int lvl = Convert.ToInt32(level);
        int cost = Convert.ToInt32(manaCost);
        int rng = Convert.ToInt32(range);
        
        var spell = new SpellType
        {
            Tag = tagStr,
            Name = nameStr,
            Level = lvl,
            ManaCost = cost,
            Range = rng,
            Sprite = sprite as Sprite,
            CanTargetEmpty = Convert.ToBoolean(canTargetEmpty),
            CanTargetAlly = Convert.ToBoolean(canTargetAlly),
            CanTargetEnemy = Convert.ToBoolean(canTargetEnemy),
            CanTargetSelf = Convert.ToBoolean(canTargetSelf),
            RequiresLineOfSight = Convert.ToBoolean(requiresLos),
            Effect = effect
        };
        
        //Console.WriteLine($"  Created spell: {tagStr} - {nameStr} (Lv{lvl}, {cost}MP)");
        
        // Process reagent list.
        var reagentVector = Builtins.ListToVector(reagents);
        if (reagentVector is object[] reagentArray)
        {
            foreach (var reagentObj in reagentArray)
            {
                if (reagentObj is ReagentType rt)
                {
                    // For now, assume 1 of each reagent.
                    // In full implementation, this would be (reagent-type quantity) pairs.
                    spell.RequiredReagents[rt] = 1;
                }
            }
            
            if (reagentArray.Length > 0)
            {
                Console.WriteLine($"    Requires {reagentArray.Length} reagents");
            }
        }
        
        // Register spell.
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, spell);      // For lookup by tag
            Magic.RegisterSpellForEnumeration(spell);     // For enumeration
        }
        
        //Console.WriteLine($"  Created spell: {tagStr} '{nameStr}' (lvl={level}, mana={manaCost})");
        
        return spell;
    }
    
    /// <summary>
    /// (kern-mk-effect tag name description 
    ///                exec-proc apply-proc remove-proc restart-proc
    ///                hook-name status-code detect-dc sprite cumulative duration)
    /// Creates an effect type.
    /// </summary>
    public static object MakeEffect(object[] args)
    {
        if (args.Length < 13)
        {
            LoadError($"kern-mk-effect: expected 13 args, got {args.Length}");
            return "nil".Eval();
        }

        // Extract parameters.
        string tagStr = ToTag(args[0]);                                    // 0: y = tag
        string nameStr = args[1]?.ToString()?.Trim('"') ?? "Unknown";      // 1: s = name
        string descStr = args[2]?.ToString()?.Trim('"') ?? "";             // 2: s = description
        object execProc = args[3];                                         // 3: c = exec_proc
        object applyProc = args[4];                                        // 4: c = apply_proc
        object removeProc = args[5];                                       // 5: c = rm_proc
        object restartProc = args[6];                                      // 6: c = restart_proc
        string hookName = args[7]?.ToString()?.Trim('"') ?? "start-of-turn"; // 7: s = hook_name
        string statusCodeStr = args[8]?.ToString() ?? " ";                 // 8: s = status_code
        int detectDc = ToInt(args[9], 0);                                  // 9: d = detect_dc
        object spriteArg = args[10];                                       // 10: p = sprite
        bool cumulative = ConvertToBool(args[11]);                         // 11: b = cumulative
        int duration = ToInt(args[12], -1);                                // 12: d = duration (-1 = permanent)

        // Convert hook name to HookId.
        HookId hookId = hookName.ToLowerInvariant() switch
        {
            "start-of-turn" or "start-of-turn-hook" => HookId.StartOfTurn,
            "on-add-hook" or "add-hook" => HookId.AddHook,
            "on-damage" or "damage-hook" => HookId.Damage,
            "on-keystroke" or "keystroke-hook" => HookId.Keystroke,
            _ => HookId.StartOfTurn  // Default
        };
        
        // Warn if unknown hook name.
        if (!hookName.ToLowerInvariant().Contains("turn") &&
            !hookName.ToLowerInvariant().Contains("add") &&
            !hookName.ToLowerInvariant().Contains("damage") &&
            !hookName.ToLowerInvariant().Contains("keystroke"))
        {
            Console.WriteLine($"  [WARNING] kern-mk-effect: unknown hook '{hookName}', defaulting to start-of-turn");
        }
        
        // Create effect.
        var effect = new Effect
        {
            Tag = tagStr,
            Name = nameStr,
            Description = descStr,
            HookId = hookId,
            DetectDC = detectDc,
            Cumulative = cumulative,
            Duration = duration
        };
        
        // Store closures (nil becomes null).
        effect.ExecClosure = IsNil(execProc) ? null : execProc;
        effect.ApplyClosure = IsNil(applyProc) ? null : applyProc;
        effect.RemoveClosure = IsNil(removeProc) ? null : removeProc;
        effect.RestartClosure = IsNil(restartProc) ? null : restartProc;
        
        // Status code is a single character.
        effect.StatusCode = statusCodeStr.Length > 0 ? statusCodeStr[0] : ' ';
        
        // Set sprite if provided.
        effect.Sprite = ResolveObject<Sprite>(spriteArg);
        
        // Register the effect.
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, effect);
            $"(define {tagStr} \"{tagStr}\")".Eval();
        }
        
        //Console.WriteLine($"  Created effect: {tagStr} - {nameStr} (hook={hookName}, dur={duration})");
        
        return effect;
    }
    
    /// <summary>
    /// (kern-mk-astral-body tag name distance min_per_phase min_per_degree 
    ///                      initial_arc initial_phase callback phase_list)
    /// Creates a celestial body (sun, moon, star).
    /// </summary>
    /// <param name="tagObj"></param>
    /// <param name="nameObj"></param>
    /// <param name="distanceObj"></param>
    /// <param name="minPerPhaseObj"></param>
    /// <param name="minPerDegreeObj"></param>
    /// <param name="initialArcObj"></param>
    /// <param name="initialPhaseObj"></param>
    /// <param name="callbackObj"></param>
    /// <param name="phaseListObj"></param>
    /// <returns></returns>
    public static object MakeAstralBody(
        object tagObj, object nameObj, object distanceObj,
        object minPerPhaseObj, object minPerDegreeObj,
        object initialArcObj, object initialPhaseObj,
        object callbackObj, object phaseListObj)
    {
        string tag = tagObj?.ToString()?.TrimStart('\'') ?? "unknown";
        string name = nameObj?.ToString() ?? "Unknown";
        int distance = Convert.ToInt32(distanceObj ?? 0);
        int minPerPhase = Convert.ToInt32(minPerPhaseObj ?? 0);
        int minPerDegree = Convert.ToInt32(minPerDegreeObj ?? 1);
        int initialArc = Convert.ToInt32(initialArcObj ?? 0);
        int initialPhase = Convert.ToInt32(initialPhaseObj ?? 0);
        
        // Convert phase list to array.
        var phaseVector = Builtins.ListToVector(phaseListObj);
        if (phaseVector is not object[] phases || phases.Length == 0)
        {
            Console.WriteLine($"[MakeAstralBody] Error: {tag} has no phases");
            return "nil".Eval();
        }
        
        var body = new AstralBody(tag, name, phases.Length)
        {
            Distance = distance,
            MinutesPerPhase = minPerPhase,
            MinutesPerDegree = minPerDegree,
            InitialArc = initialArc,
            InitialPhase = initialPhase,
            Arc = initialArc,
            PhaseIndex = initialPhase
        };
        
        // Store callback if provided.
        if (callbackObj != null && callbackObj is not bool b)
        {
            body.PhaseChangeCallback = callbackObj;
        }
        else if (callbackObj is bool bVal && bVal == false)
        {
            // nil in Scheme becomes false - no callback
            body.PhaseChangeCallback = null;
        }
        
        // Parse each phase: (list sprite maxlight "phase_name")
        for (int i = 0; i < phases.Length; i++)
        {
            var phaseData = Builtins.ListToVector(phases[i]);
            if (phaseData is object[] pd && pd.Length >= 3)
            {
                body.Phases[i] = new Phase
                {
                    Sprite = pd[0] as Sprite,
                    MaxLight = Convert.ToInt32(pd[1] ?? 0),
                    Name = pd[2]?.ToString() ?? $"Phase {i}"
                };
            }
            else
            {
                Console.WriteLine($"[MakeAstralBody] Warning: {tag} phase {i} malformed");
                body.Phases[i] = new Phase { Name = $"Phase {i}", MaxLight = 0 };
            }
        }
        
        // Add to sky.
        var session = Phantasma.MainSession;
        if (session != null)
        {
            session.Sky.AddAstralBody(body);
        }
        else
        {
            Console.WriteLine("[MakeAstralBody] Warning: No main session");
        }
        
        // Register in global namespace so Scheme can reference by tag.
        Phantasma.RegisterObject(tag, body);
        
        Console.WriteLine($"  Created astral body: {tag} '{name}' (distance={distance}, phases={phases.Length})");
        
        return body;
    }
    
    /// <summary>
    /// (kern-mk-vehicle-type tag name sprite map ordnance
    ///                       vulnerable kills-occupants must-turn
    ///                       mv-desc mv-sound
    ///                       tailwind-penalty headwind-penalty crosswind-penalty
    ///                       max-hp speed mmode)
    /// Creates a vehicle type definition.
    /// </summary>
    public static object MakeVehicleType(
        object tag, object name, object sprite, object map, object ordnance,
        object vulnerable, object killsOccupants, object mustTurn,
        object mvDesc, object mvSound,
        object tailwindPenalty, object headwindPenalty, object crosswindPenalty,
        object maxHp, object speed, object mmode)
    {
        string tagStr = tag?.ToString()?.TrimStart('\'') ?? "";
        
        if (string.IsNullOrEmpty(tagStr))
        {
            Console.WriteLine("kern-mk-vehicle-type: missing tag");
            return "#f".Eval();
        }
        
        // Resolve sprite.
        Sprite? spr = null;
        if (sprite is Sprite s)
            spr = s;
        else if (sprite is string sprTag && !string.IsNullOrEmpty(sprTag) && sprTag != "nil")
            spr = Phantasma.GetRegisteredObject(sprTag) as Sprite;
        
        // Resolve terrain map (for combat) - TerrainMap is a struct.
        TerrainMap? combatMap = null;
        if (map is TerrainMap tm)
            combatMap = tm;
        else if (map is string mapTag && !string.IsNullOrEmpty(mapTag) && mapTag != "nil")
        {
            var mapObj = Phantasma.GetRegisteredObject(mapTag);
            if (mapObj is TerrainMap tmResolved)
                combatMap = tmResolved;
        }
        
        // Resolve ordnance (weapon).
        ArmsType? arms = null;
        if (ordnance is ArmsType at)
            arms = at;
        else if (ordnance is string armsTag && !string.IsNullOrEmpty(armsTag) && armsTag != "nil")
            arms = Phantasma.GetRegisteredObject(armsTag) as ArmsType;
        
        // Resolve movement mode - MovementMode is a struct.
        MovementMode? movementMode = null;
        if (mmode is MovementMode mm)
            movementMode = mm;
        else if (mmode is string mmTag && !string.IsNullOrEmpty(mmTag) && mmTag != "nil")
        {
            var mmObj = Phantasma.GetRegisteredObject(mmTag);
            if (mmObj is MovementMode mmResolved)
                movementMode = mmResolved;
        }
        
        // Create the vehicle type using the full constructor.
        var vehicleType = new VehicleType(
            tag: tagStr,
            name: name?.ToString() ?? tagStr,
            sprite: spr,
            combatMap: combatMap,
            ordnance: arms,
            vulnerable: ConvertToBool(vulnerable),
            killsOccupants: ConvertToBool(killsOccupants),
            mustTurn: ConvertToBool(mustTurn),
            mvDesc: mvDesc?.ToString() ?? "rides",
            mvSound: mvSound,  // TODO: Implement sound.
            tailwindPenalty: Convert.ToInt32(tailwindPenalty ?? 1),
            headwindPenalty: Convert.ToInt32(tailwindPenalty ?? 1),
            crosswindPenalty: Convert.ToInt32(crosswindPenalty ?? 1),
            maxHp: Convert.ToInt32(maxHp ?? 100),
            speed: Convert.ToInt32(speed ?? 1),
            mmode: movementMode
        );
        
        // Register with Phantasma.
        Phantasma.RegisterObject(tagStr, vehicleType);
        
        //Console.WriteLine($"  Created vehicle type: {tagStr} '{vehicleType.Name}'");
        
        return vehicleType;
    }
    
    /// <summary>
    /// (kern-mk-vehicle type facing hp)
    /// </summary>
    public static object MakeVehicle(object type, object facing, object hp)
    {
        // Resolve vehicle type.
        VehicleType? vehicleType = type as VehicleType;
    
        if (type is VehicleType vt)
            vehicleType = vt;
        else if (type is string typeTag && !string.IsNullOrEmpty(typeTag) && typeTag != "nil")
            vehicleType = Phantasma.GetRegisteredObject(typeTag) as VehicleType;
            
        if (vehicleType == null)
        {
            Console.WriteLine("[kern-mk-vehicle] Error: null vehicle type.");
            return "#f".Eval();
        }
            
        int facingInt = Convert.ToInt32(facing ?? Common.NORTH);
        int hpInt = Convert.ToInt32(hp ?? vehicleType.MaxHp);
            
        var vehicle = vehicleType.CreateInstance(facingInt, hpInt);
            
        //Console.WriteLine($"  Created vehicle: {vehicleType.Name} (facing={facingInt}, hp={hpInt})");
            
        return vehicle;
    }
    /// <summary>
    /// (kern-mk-sound tag filename)
    /// Creates a sound from a WAV file.
    /// </summary>
    /// <param name="tag">Scheme symbol identifier (e.g., 'snd-footstep)</param>
    /// <param name="filename">Path to WAV file (e.g., "sounds/footstep.wav")</param>
    /// <returns>The Sound object, or unspecified if loading failed.</returns>
    /// <example>
    /// Scheme usage:
    /// (define snd-footstep (kern-mk-sound 'snd-footstep "sounds/footstep.wav"))
    /// </example>
    public static object MakeSound(object tag, object filename)
    {
        // Extract tag string.
        string tagStr = tag?.ToString()?.TrimStart('\'') ?? "unknown-sound";
        
        // Extract filename string.
        string? filenameStr = filename?.ToString();
        
        if (string.IsNullOrEmpty(filenameStr))
        {
            Console.WriteLine($"[kern-mk-sound] {tagStr}: null or empty filename");
            return "nil".Eval();
        }
        
        // Load the sound.
        var sound = SoundManager.Instance.LoadSound(tagStr, filenameStr);
        
        if (sound == null)
        {
            Console.WriteLine($"[kern-mk-sound] {tagStr}: failed to load '{filenameStr}'");
            return "nil".Eval();
        }
        
        // Register with Phantasma for lookup by tag.
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, sound);
            $"(define {tagStr} \"{tagStr}\")".Eval();
        }
        
        //Console.WriteLine($"  Created sound: {tagStr} ('{filenameStr}')");
        
        return sound;
    }
    
    /// <summary>
    /// (kern-mk-ptable row1 row2 ...)
    /// Creates a passability table from a list of lists.
    /// Each row corresponds to a passability class (terrain type).
    /// Each column corresponds to a movement mode (walking, swimming, etc).
    /// Values are movement costs (higher = slower, 255 = impassable).
    /// </summary>
    /// <remarks>
    /// Nazghul kern.c signature:
    ///   KERN_API_CALL(kern_mk_ptable) - args is list of rows
    ///   Each row is a list of integers (movement costs per mode)
    /// </remarks>
    public static object MakePassabilityTable(object[] args)
    {
        // Each arg is a list of costs for one passability class.
        var rows = new List<List<int>>();
    
        foreach (var arg in args)
        {
            var row = ConvertToIntList(arg);
            rows.Add(row);
        }
    
        int numPClass = rows.Count;
        int numMMode = rows.Count > 0 ? rows[0].Count : 0;
        
        Console.WriteLine($"[kern-mk-ptable] Creating table: {numMMode} modes x {numPClass} pclasses");
    
        var ptable = new PassabilityTable(numMMode, numPClass);
    
        for (int pclass = 0; pclass < numPClass; pclass++)
        {
            var rowStr = string.Join(",", rows[pclass]);
            Console.WriteLine($"  pclass {pclass}: [{rowStr}]");
        
            for (int mmode = 0; mmode < numMMode; mmode++)
                if (mmode < rows[pclass].Count)
                    ptable.SetCost(mmode, pclass, rows[pclass][mmode]);
        }
    
        Phantasma.RegisterObject("ptable", ptable);
        $"(define ptable \"ptable\")".Eval();
        Console.WriteLine($"[kern-mk-ptable] Registered ptable in object registry");
        
        // Verify it was stored.
        var verify = Phantasma.GetRegisteredObject("ptable");
        Console.WriteLine($"[kern-mk-ptable] Verify lookup: {verify?.GetType().Name ?? "NULL"}");
        
        return "nil".Eval();
    }
    
    /// <summary>
    /// (kern-mk-dtable row1 row2 ...)
    /// Creates a diplomacy table from a list of lists.
    /// Each row/column corresponds to a faction.
    /// Values are diplomacy levels (negative = hostile, 0 = neutral, positive = allied).
    /// The table should be square (same number of rows and columns).
    /// </summary>
    /// <remarks>
    /// Nazghul kern.c signature:
    ///   KERN_API_CALL(kern_mk_dtable) - args is list of rows
    ///   Returns a pointer to the dtable (for use with kern-dtable-* functions)
    /// </remarks>
    public static object MakeDiplomacyTable(object[] args)
    {
        if (args == null || args.Length == 0)
        {
            Console.Error.WriteLine("[kern-mk-dtable] Error: 0 factions given");
            return "nil".Eval();
        }
    
        // Each arg is a list of diplomacy levels for one faction.
        var rows = new List<List<int>>();
    
        foreach (var arg in args)
        {
            var row = ConvertToIntList(arg);
            rows.Add(row);
        }
    
        int numFactions = rows.Count;
    
        // Validate square table.
        if (rows[0].Count != numFactions)
        {
            Console.Error.WriteLine($"[kern-mk-dtable] Error: # of rows ({numFactions}) and columns ({rows[0].Count}) must be same");
            return "nil".Eval();
        }
    
        // Validate all rows have correct number of columns.
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].Count < numFactions)
            {
                Console.Error.WriteLine($"[kern-mk-dtable] Error: row {i} has only {rows[i].Count} columns (expected {numFactions})");
                return "nil".Eval();
            }
        }
    
        // Create the diplomacy table.
        var dtable = new DiplomacyTable(numFactions);
    
        // Fill in the values.
        for (int f1 = 0; f1 < numFactions; f1++)
        {
            for (int f2 = 0; f2 < numFactions; f2++)
            {
                int level = rows[f1][f2];
                dtable.Set(f1, f2, level);
            }
        }
    
        Phantasma.RegisterObject("dtable", dtable);
        $"(define dtable \"dtable\")".Eval();
    
        //Console.WriteLine($"[kern-mk-dtable] Created {numFactions}x{numFactions} diplomacy table");
    
        return dtable;
    }
    
    /// <summary>
    /// (kern-mk-field-type tag name sprite light duration pclass [effect-proc])
    /// Creates a field type definition.
    /// 
    /// Example:
    /// (kern-mk-field-type 'F_fire "fire" s_field_fire 1 5 pclass-field burn-proc)
    /// </summary>
    /// <param name="tag">Symbol identifier (e.g., 'F_fire)</param>
    /// <param name="name">Display name for the field</param>
    /// <param name="sprite">Sprite object for rendering</param>
    /// <param name="light">Light level emitted (0 = no light)</param>
    /// <param name="duration">Default duration in turns (-1 = permanent)</param>
    /// <param name="pclass">Passability class (for movement costs)</param>
    /// <param name="effect-proc">Optional Scheme closure called when stepped on</param>
    public static object MakeFieldType(object tag, object name, object sprite, 
                                        object light, object duration, object pclass, 
                                        object effect = null)
    {
        string tagStr = tag?.ToString()?.TrimStart('\'') ?? "";
        string nameStr = name?.ToString() ?? tagStr;
        
        if (string.IsNullOrEmpty(tagStr))
        {
            Console.WriteLine("[ERROR] kern-mk-field-type: missing tag");
            return "#f".Eval();
        }
        
        // Resolve sprite.
        Sprite? spr = null;
        if (sprite is Sprite s)
            spr = s;
        else if (sprite is string sprTag && !string.IsNullOrEmpty(sprTag))
            spr = Phantasma.GetRegisteredObject(sprTag.TrimStart('\'').Trim('"')) as Sprite;
        
        // Parse numeric parameters.
        int lightVal = Convert.ToInt32(light ?? 0);
        int durationVal = Convert.ToInt32(duration ?? -1);
        int pclassVal = Convert.ToInt32(pclass ?? 0);
        
        // Create the field type.
        var fieldType = new FieldType(tagStr, nameStr, spr, lightVal, durationVal, pclassVal, effect);
        
        // Register with Phantasma.
        Phantasma.RegisterObject(tagStr, fieldType);
        
        // Define in Scheme environment.
        try
        {
            $"(define {tagStr} \"{tagStr}\")".Eval();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Could not define {tagStr} in Scheme: {ex.Message}");
        }
        
        //Console.WriteLine($"  Created field type: {tagStr} '{nameStr}' (light={lightVal}, duration={durationVal}, pclass={pclassVal})");
        
        return fieldType;
    }
    
    /// <summary>
    /// (kern-mk-party-type tag name sprite formation groups)
    /// Creates a party type definition.
    /// 
    /// Example:
    /// (kern-mk-party-type 'pt_goblins "Goblins" s_goblin formation-wedge
    ///   (list
    ///     (list sp_goblin s_goblin "2d3" mk-goblin)
    ///     (list sp_goblin_shaman s_shaman "1d2-1" mk-shaman)))
    /// </summary>
    /// <param name="tag">Symbol identifier (e.g., 'pt_goblin_patrol)</param>
    /// <param name="name">Display name for the party</param>
    /// <param name="sprite">Sprite object for rendering on world map</param>
    /// <param name="formation">Formation object for combat positioning</param>
    /// <param name="groups">List of group definitions, each being</param>
    public static object MakePartyType(object tag, object name, object sprite, 
                                        object formation, object groups)
    {
        string tagStr = tag?.ToString()?.TrimStart('\'') ?? "";
        string nameStr = name?.ToString() ?? tagStr;
        
        if (string.IsNullOrEmpty(tagStr))
        {
            Console.WriteLine("[ERROR] kern-mk-party-type: missing tag");
            return "#f".Eval();
        }
        
        // Resolve sprite.
        Sprite? spr = null;
        if (sprite is Sprite s)
            spr = s;
        else if (sprite is string sprTag && !string.IsNullOrEmpty(sprTag))
            spr = Phantasma.GetRegisteredObject(sprTag.TrimStart('\'').Trim('"')) as Sprite;
        
        // Create the party type.
        var partyType = new PartyType(tagStr, nameStr, spr);
        
        // Resolve formation.
        if (formation is Formation f)
            partyType.Formation = f;
        else if (formation is string formTag && !string.IsNullOrEmpty(formTag))
            partyType.Formation = Phantasma.GetRegisteredObject(formTag.TrimStart('\'').Trim('"')) as Formation;
        
        // Parse groups list.
        int groupCount = 0;
        if (groups != null)
        {
            try
            {
                // Convert to vector for iteration.
                var groupsVector = Builtins.ListToVector(groups);
                
                if (groupsVector is object[] groupsArray)
                {
                    foreach (var groupObj in groupsArray)
                    {
                        if (groupObj == null) continue;
                        
                        // Each group is (species sprite dice factory).
                        var speciesObj = Builtins.Car(groupObj);
                        var rest1 = Builtins.Cdr(groupObj);
                        var groupSpriteObj = Builtins.Car(rest1);
                        var rest2 = Builtins.Cdr(rest1);
                        var diceObj = Builtins.Car(rest2);
                        var rest3 = Builtins.Cdr(rest2);
                        var factoryObj = Builtins.Car(rest3);
                        
                        // Resolve species.
                        Species species = default;
                        if (speciesObj is Species sp)
                            species = sp;
                        else if (speciesObj is string spTag)
                        {
                            var spObj = Phantasma.GetRegisteredObject(spTag.TrimStart('\'').Trim('"'));
                            if (spObj is Species resolvedSp)
                                species = resolvedSp;
                        }
                        
                        // Resolve sprite.
                        Sprite? groupSprite = null;
                        if (groupSpriteObj is Sprite gs)
                            groupSprite = gs;
                        else if (groupSpriteObj is string gsTag)
                            groupSprite = Phantasma.GetRegisteredObject(gsTag.TrimStart('\'').Trim('"')) as Sprite;
                        
                        // Get dice string.
                        string dice = diceObj?.ToString()?.Trim('"') ?? "1";
                        
                        // Factory is stored as-is (closure).
                        partyType.AddGroup(species, groupSprite, dice, factoryObj);
                        groupCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Error parsing groups for {tagStr}: {ex.Message}");
            }
        }
        
        // Register with Phantasma.
        Phantasma.RegisterObject(tagStr, partyType);
        
        // Define in Scheme environment.
        try
        {
            $"(define {tagStr} \"{tagStr}\")".Eval();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Could not define {tagStr} in Scheme: {ex.Message}");
        }
        
        //Console.WriteLine($"  Created party type: {tagStr} '{nameStr}' ({groupCount} groups)");
        
        return partyType;
    }
    
    /// <summary>
    /// (kern-mk-sched tag appointments...)
    /// Creates a schedule for NPC daily routines.
    /// 
    /// Activity names: "idle", "working", "sleeping", "commuting", "eating", "drunk"
    /// 
    /// Example:
    /// (kern-mk-sched 'sch_smith
    ///   (list 6 0  (list 10 10 1 1) "commuting")
    ///   (list 8 0  (list 15 12 3 3) "working")
    ///   (list 18 0 (list 10 10 1 1) "commuting")
    ///   (list 19 0 (list 5 5 2 2) "eating")
    ///   (list 22 0 (list 5 5 2 2) "sleeping"))
    /// </summary>
    /// <param name="tag">Symbol identifier (e.g., 'sch_blacksmith)</param>
    /// <param name="appointments">Variable number of appointment definitions, each being</param>
    public static object MakeSchedule(object tag, params object[] appointments)
    {
        string tagStr = ToCleanString(tag) ?? "";
        
        if (string.IsNullOrEmpty(tagStr))
        {
            Console.WriteLine("[ERROR] kern-mk-sched: missing tag");
            return "#f".Eval();
        }
        
        var schedule = new Schedule(tagStr);
        
        foreach (var apptObj in appointments)
        {
            if (apptObj == null) continue;
            
            try
            {
                // Each appointment is (hour minute zone-or-rect activity)
                var hourObj = Builtins.Car(apptObj);
                var rest1 = Builtins.Cdr(apptObj);
                var minObj = Builtins.Car(rest1);
                var rest2 = Builtins.Cdr(rest1);
                var zoneOrRectObj = Builtins.Car(rest2);
                var rest3 = Builtins.Cdr(rest2);
                var activityObj = Builtins.Car(rest3);
                
                int hour = ToInt(hourObj, 0);
                int minute = ToInt(minObj, 0);
                
                // Parse zone/rect - either (x y w h) list or zone symbol
                int x = 0, y = 0, w = 1, h = 1;
                if (zoneOrRectObj != null)
                {
                    if (IsSymbol(zoneOrRectObj))
                    {
                        // Zone symbol lookup
                        var zone = Phantasma.GetRegisteredObject(zoneOrRectObj.ToString() ?? "");
                        if (zone != null)
                        {
                            var zoneType = zone.GetType();
                            var xProp = zoneType.GetProperty("X") ?? zoneType.GetProperty("x");
                            var yProp = zoneType.GetProperty("Y") ?? zoneType.GetProperty("y");
                            var wProp = zoneType.GetProperty("W") ?? zoneType.GetProperty("Width") ?? zoneType.GetProperty("w");
                            var hProp = zoneType.GetProperty("H") ?? zoneType.GetProperty("Height") ?? zoneType.GetProperty("h");
                            
                            if (xProp != null) x = Convert.ToInt32(xProp.GetValue(zone) ?? 0);
                            if (yProp != null) y = Convert.ToInt32(yProp.GetValue(zone) ?? 0);
                            if (wProp != null) w = Convert.ToInt32(wProp.GetValue(zone) ?? 1);
                            if (hProp != null) h = Convert.ToInt32(hProp.GetValue(zone) ?? 1);
                        }
                    }
                    else
                    {
                        // Parse as (x y w h) list
                        try
                        {
                            x = ToInt(Builtins.Car(zoneOrRectObj), 0);
                            var r1 = Builtins.Cdr(zoneOrRectObj);
                            y = ToInt(Builtins.Car(r1), 0);
                            var r2 = Builtins.Cdr(r1);
                            w = ToInt(Builtins.Car(r2), 1);
                            var r3 = Builtins.Cdr(r2);
                            h = ToInt(Builtins.Car(r3), 1);
                        }
                        catch { /* Use defaults */ }
                    }
                }
                
                // Parse activity - handle both strings and symbols
                string actStr = (ToCleanString(activityObj) ?? "idle").ToLower();
                Activity activity = actStr switch
                {
                    "idle" => Activity.Idle,
                    "working" => Activity.Working,
                    "sleeping" => Activity.Sleeping,
                    "commuting" => Activity.Commuting,
                    "eating" => Activity.Eating,
                    "drunk" => Activity.Drunk,
                    "wandering" => Activity.Wandering,
                    _ => Activity.Idle
                };
                
                schedule.AddAppointment(hour, minute, x, y, w, h, activity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Error parsing appointment for {tagStr}: {ex.Message}");
            }
        }
        
        Phantasma.RegisterObject(tagStr, schedule);
        
        try
        {
            $"(define {tagStr} \"{tagStr}\")".Eval();
        }
        catch { }
        
        //Console.WriteLine($"  Created schedule: {tagStr} ({schedule.Appointments.Count} appointments)");
        
        return schedule;
    }
}
