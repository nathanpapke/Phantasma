using System;
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
        dynamic ss = spriteSet;
        
        // Load the sprite sheet image
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
            Tag = tag?.ToString(),  // The image filename from sprite set
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
        if (tag != null)
        {
            Phantasma.RegisterObject(tag.ToString(), sprite);
        }
        
        return sprite;
    }
    
    public static object MakeSpriteSet(object tag, object width, object height, object rows, object cols, object offx, object offy, object filename) 
    {
        // For now, just store the metadata in a dictionary or anonymous object
        // The actual sprite loading would happen elsewhere
        var spriteSetData = new 
        {
            Tag = tag?.ToString(),
            Width = Convert.ToInt32(width ?? 32),
            Height = Convert.ToInt32(height ?? 32),
            Rows = Convert.ToInt32(rows ?? 1),
            Cols = Convert.ToInt32(cols ?? 1),
            OffsetX = Convert.ToInt32(offx ?? 0),
            OffsetY = Convert.ToInt32(offy ?? 0),
            Filename = filename?.ToString()
        };
        
        // Return the metadata - MakeSprite will use this
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
    public static object MakeTerrain(object tag, object name, object pclass, object sprite, object alpha, object light)
    {
        string tagStr = tag?.ToString() ?? "";
        string nameStr = name?.ToString() ?? "";
        
        var terrain = new Terrain
        {
            Name = name?.ToString(),
            Color = GetTerrainColor(tagStr, nameStr),
            PassabilityClass = pclass == null ? 0 : (int)Convert.ToDouble(pclass),
            //IsPassable
            //MovementCost
            //IsHazardous
            //Effect
            Light = Convert.ToInt32(light),
            Alpha = Convert.ToByte(alpha),
            //Transparent
            //DisplayChar
            Sprite = sprite as Sprite
        };
            
        // Register with Phantasma for lookup.
        if (tag != null)
        {
            Phantasma.RegisterObject(tag.ToString(), terrain);
        }
        
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
    
        Console.WriteLine($"  Created terrain type: {terrain.Name}");
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
                // Extract glyph (first) and terrain (second).
                var glyph = Builtins.Car(pairObj);
                var rest = Builtins.Cdr(pairObj);
                var terrain = Builtins.Car(rest);
                
                string glyphStr = glyph?.ToString()?.Trim('"', '\'') ?? "";
                
                if (terrain is Terrain t && !string.IsNullOrEmpty(glyphStr))
                {
                    palette.AddMapping(glyphStr, t);
                    count++;
                }
            }
        }
        
        // Register palette for later use.
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, palette);
        }
        
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
        var pal = palette as TerrainPalette;
        
        if (pal == null)
        {
            return Builtins.Unspecified;
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
        }
        
        return map;
    }
    
    /// <summary>
    /// (kern-mk-place tag name sprite map wraps underground wild combat
    ///                subplaces neighbors contents hooks entrances)
    /// Creates a place (map/location).
    /// </summary>
    public static object MakePlace(
        object tag, object name,
        object sprite, object map,
        object wraps, object underground, object wild, object combat,
        object subplaces, object neighbors, object contents,
        object hooks, object entrances)
    {
        string tagStr = tag?.ToString()?.TrimStart('\'') ?? "unknown";
        string nameStr = name?.ToString() ?? "Unnamed Place";

        var terrainMap = map is TerrainMap ? (TerrainMap)map : default;
        //if (terrainMap == null)
        {
            //Console.WriteLine($"[ERROR] kern-mk-place {tagStr}: invalid terrain map");
            //return Builtins.Unspecified;
        }
        
        Console.WriteLine($"  Creating place: {tagStr} - {nameStr}");
        
        var place = new Place
        {
            Tag = tagStr,
            Name = nameStr,
            Sprite = sprite as Sprite,
            Width = terrainMap.Width,
            Height = terrainMap.Height,
            TerrainGrid = terrainMap.TerrainGrid,
            Wraps = Convert.ToBoolean(wraps),
            IsUnderground = Convert.ToBoolean(underground),
            IsWilderness = Convert.ToBoolean(wild),
            CombatEnabled = Convert.ToBoolean(combat)
        };
        
        Console.WriteLine($"    Place created ({place.Width}x{place.Height})");
        
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, place);
        }
        
        return place;
    }
    
    /// <summary>
    /// (kern-mk-mmode tag name index)
    /// Creates a movement mode.
    /// </summary>
    public static object MakeMovementMode(object tag, object name, object index)
    {
        string tagStr = tag?.ToString()?.TrimStart('\'');
        string nameStr = name?.ToString() ?? "Unknown";
        int indexInt = Convert.ToInt32(index ?? 0);
        
        var mmode = new MovementMode(tagStr, nameStr, indexInt);
        
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, mmode);
        }
        
        Console.WriteLine($"  Created mmode: {nameStr} (index={indexInt})");
        
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
    public static object MakeSpecies(
        object tag, object name,
        object str, object intl, object dex, object spd, object vr,
        object mmode,
        object hpmod, object hpmult, object mpmod, object mpmult,
        object sleepSprite, object weapon, object visible,
        object damageSound, object walkingSound, object onDeath,
        object xpval, object slots, object spells)
    {
        string tagStr = tag?.ToString()?.TrimStart('\'');
        
        // Get Movement Mode
        MovementMode movementMode;
        if (mmode is MovementMode mm)
            movementMode = mm;
        else if (mmode is int i)
            movementMode = new MovementMode(null, "Walking", i);
        else
            movementMode = new MovementMode("mmode-walk", "Walking", 0);
        
        var species = new Species
        {
            Tag = tagStr,
            Name = name?.ToString() ?? "Unknown",
            Str = Convert.ToInt32(str ?? 10),
            Intl = Convert.ToInt32(intl ?? 10),
            Dex = Convert.ToInt32(dex ?? 10),
            Spd = Convert.ToInt32(spd ?? 10),
            Vr = Convert.ToInt32(vr ?? 10),
            MovementMode = movementMode,
            HpMod = Convert.ToInt32(hpmod ?? 10),
            HpMult = Convert.ToInt32(hpmult ?? 5),
            MpMod = Convert.ToInt32(mpmod ?? 5),
            MpMult = Convert.ToInt32(mpmult ?? 2),
            SleepSprite = sleepSprite as Sprite,
            Weapon = weapon as ArmsType,
            Visible = ConvertToBool(visible ?? true),
            // damageSound, walkingSound - TODO when sound system implemented
            // onDeath - TODO when closure system implemented
            XpVal = Convert.ToInt32(xpval ?? 10)
            // slots, spells - TODO: parse lists when needed
        };
        
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, species);
        }
        
        Console.WriteLine($"  Created species: {species.Name} (str={species.Str}, dex={species.Dex}, hp={species.HpMod}+{species.HpMult}/lvl)");
        
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
        }
        
        Console.WriteLine($"  Created occupation: {occ.Name} (magic={occ.Magic:F1}, hp+{occ.HpMod}+{occ.HpMult}/lvl)");
        
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
    public static object MakeCharacter(
        object tag, object name, object species, object occ, object sprite,
        object baseFaction,
        object str, object intl, object dex,
        object hpmod, object hpmult, object mpmod, object mpmult,
        object hp, object xp, object mp, object lvl,
        object dead,
        object conv, object sched, object ai, object inventory,
        object readiedList, object hooksList)
    {
        string tagStr = tag?.ToString()?.TrimStart('\'');
        
        var character = new Character();
        character.SetName(name?.ToString() ?? "Unknown");
        
        // Set sprite.
        if (sprite is Sprite s)
            character.CurrentSprite = s;
        
        // Set species if available.
        if (species is Species sp)
            character.Species = sp;
        
        // Set occupation if available.
        if (occ is Occupation o)
            character.Occupation = o;
        
        // Set base faction.
        character.SetBaseFaction(Convert.ToInt32(baseFaction ?? 0));
        
        // Set base stats.
        character.Strength = Convert.ToInt32(str ?? 10);
        character.Intelligence = Convert.ToInt32(intl ?? 10);
        character.Dexterity = Convert.ToInt32(dex ?? 10);
        
        // HP Calculation
        int baseHpMod = Convert.ToInt32(hpmod ?? 10);
        int hpPerLevel = Convert.ToInt32(hpmult ?? 5);
        int currentHp = Convert.ToInt32(hp ?? 0);
        int level = Convert.ToInt32(lvl ?? 1);
        
        character.MaxHP = baseHpMod + (hpPerLevel * level);
        character.HP = currentHp > 0 ? currentHp : character.MaxHP;
        
        // MP Calculation
        int baseMpMod = Convert.ToInt32(mpmod ?? 5);
        int mpPerLevel = Convert.ToInt32(mpmult ?? 2);
        int currentMp = Convert.ToInt32(mp ?? 0);
        
        character.MaxMP = baseMpMod + (mpPerLevel * level);
        character.MP = currentMp > 0 ? currentMp : character.MaxMP;
        
        // XP and Level
        character.Experience = Convert.ToInt32(xp ?? 0);
        character.Level = level;
        
        // Dead Flag
        //character.IsDead =  ConvertToBool(dead);
        // Character IsDead if HP == 0
        
        // Store conversation closure.
        if (conv != null && !(conv is bool b && b == false))
        {
            character.Conversation = conv;
        }
        // sched - schedule
        // TODO: Implement scheduling system.
        // ai - AI closure
        // TODO: Implement AI system.
        // inventory - Container
        // TODO: Implement inventory.
        
        // readiedList - list of readied arms
        // TODO: Implement equipment.
        // Process readied arms list
        if (readiedList is Cons armsList)   
        {
            // TODO: Iterate through and ready each arm.
            // while (armsList != null) { ... }
        }
        
        // hooksList - list of effect hooks (TODO: implement hooks)
        if (hooksList is Cons hooks)
        {
            // TODO: Process hooks.
        }
        
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, character);
        }
        
        Console.WriteLine($"  Created character: {character.Name} (str={character.Strength}, hp={character.HP}/{character.MaxHP}, lvl={character.Level})");
        
        return character;
    }
    
    /// <summary>
    /// (kern-mk-obj type count)
    /// Creates an object instance from a type.
    /// </summary>
    public static object MakeObject(object type, object count)
    {
        if (type is ObjectType objType)
        {
            var item = new Item
            {
                Type = objType,
                Count = Convert.ToInt32(count ?? 1)
            };
        
            // Copy properties from type.
            item.Name = objType.Name;
            if (objType.Sprite != null)
                item.Type.Sprite = objType.Sprite;
        
            Console.WriteLine($"  Created object: {objType.Name} x{item.Count}");
        
            return item;
        }
    
        Console.WriteLine("  [WARNING] kern-mk-obj: invalid type");
        return Builtins.Unspecified;
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
    
        // Store interaction handler closure for later use.
        if (interactionHandler != null && !(interactionHandler is bool b && b == false))
            objType.InteractionHandler = interactionHandler;
    
        if (!string.IsNullOrEmpty(tagStr))
            Phantasma.RegisterObject(tagStr, objType);
    
        Console.WriteLine($"  Created object type: {tagStr} '{objType.Name}' (layer={objType.Layer}, caps={objType.Capabilities})");
    
        return objType;
    }

    /// <summary>
    /// (kern-mk-arms-type tag name sprite to-hit damage armor defend
    ///                    slots hands range rap missile thrown ubiq
    ///                    weight fire-sound gifc-cap gifc)
    /// Creates a weapon or armor type.
    /// </summary>
    public static object MakeArmsType(
        object tag, object name, object sprite,
        object toHit, object damage, object armor, object defend,
        object slots, object hands, object range, object rap,
        object missile, object thrown, object ubiq, object weight,
        object fireSound, object gifcCap, object gifc)
    {
        string tagStr = tag?.ToString()?.TrimStart('\'') ?? "unknown";
        string nameStr = name?.ToString() ?? tagStr;
        
        // Get dice strings
        string toHitDice = toHit?.ToString() ?? "0";
        string damageDice = damage?.ToString() ?? "0";
        string armorDice = armor?.ToString() ?? "0";
        string defendDice = defend?.ToString() ?? "0";
        
        // Validate dice notation
        if (!Dice.IsValid(toHitDice))
        {
            LoadError($"kern-mk-arms-type {tagStr}: bad to-hit dice '{toHitDice}'");
            return Builtins.Unspecified;
        }
        if (!Dice.IsValid(damageDice))
        {
            LoadError($"kern-mk-arms-type {tagStr}: bad damage dice '{damageDice}'");
            return Builtins.Unspecified;
        }
        if (!Dice.IsValid(armorDice))
        {
            LoadError($"kern-mk-arms-type {tagStr}: bad armor dice '{armorDice}'");
            return Builtins.Unspecified;
        }
        if (!Dice.IsValid(defendDice))
        {
            LoadError($"kern-mk-arms-type {tagStr}: bad defend dice '{defendDice}'");
            return Builtins.Unspecified;
        }
        
        // Use the full constructor - it sets all protected properties internally
        var armsType = new ArmsType(
            tag: tagStr,
            name: nameStr,
            sprite: sprite as Sprite,
            slotMask: Convert.ToInt32(slots ?? 0x01),
            toHitDice: toHitDice,
            toDefendDice: defendDice,
            numHands: Convert.ToInt32(hands ?? 1),
            range: Convert.ToInt32(range ?? 1),
            weight: Convert.ToInt32(weight ?? 0),
            damageDice: damageDice,
            armorDice: armorDice,
            requiredActionPoints: Convert.ToInt32(rap ?? 1),
            thrown: Convert.ToBoolean(thrown),
            ubiquitousAmmo: Convert.ToBoolean(ubiq),
            missileType: missile as ArmsType  // null if not provided
        );
        
        // Set Scheme variable so 't_sword holds this weapon.
        //"(set-global! {0} {1})".Eval(tagStr, armsType);
        
        Console.WriteLine($"  Created arms type '{tagStr}': {nameStr}");
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
    
        Console.WriteLine($"  Created container");
    
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
    
        Console.WriteLine($"  Created party (faction={party.Faction})");
    
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
        
        // Register the party
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, party);
        }
        Phantasma.RegisterObject(KEY_PLAYER_PARTY, party);
        
        // Register the first member as the player character
        if (firstMember != null)
        {
            Phantasma.RegisterObject(KEY_PLAYER_CHARACTER, firstMember);
            Console.WriteLine($"  Set player character: {firstMember.GetName()}");
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
        
        Console.WriteLine($"  Created reagent type: {tagStr} - {nameStr}");
        
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
        
        Console.WriteLine($"  Created spell: {tagStr} - {nameStr} (Lv{lvl}, {cost}MP)");
        
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
        
        return spell;
    }
    
    public static object MakeSound(object args)
    {
        // TODO: Implement
        return Builtins.Unspecified;
    }
}
