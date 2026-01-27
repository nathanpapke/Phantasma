using System;
using System.Collections.Generic;
using System.Linq;
using IronScheme.Runtime;
using IronScheme.Scripting;

namespace Phantasma.Models;

public class Place
{
    public string Tag { get; set; } = "";
    public string Name { get; set; }
    public Sprite? Sprite { get; set; }
    
    public Terrain[,] TerrainGrid { get; set; }
    public int Width { get; set; } //  => TerrainGrid?.GetLength(0) ?? 0;
    public int Height { get; set; } // => TerrainGrid?.GetLength(1) ?? 0;
    
    public Location Location { get; set; }      // Parent location (for subplaces)
    public Place? Above { get; set; }           // Vertical neighbor (stairs up)
    public Place? Below { get; set; }           // Vertical neighbor (stairs down)
    public List<Place> Subplaces { get; set; } = new();     // Child places (towns in wilderness)
    
    // Scale for time passage (wilderness = larger scale)
    public int Scale { get; set; } = 1;  // Nazghul: WILDERNESS_SCALE vs NON_WILDERNESS_SCALE
    
    public int[,] EdgeEntrances { get; } = new int[9, 2];   // [9 directions][x/y]
    
    // Pre-entry hook (Scheme closure called before entering)
    public object? PreEntryHook { get; set; }
    
    // Dirty flag for rendering optimization
    public bool Dirty { get; set; } = true;
    
    // For save/load system
    public int SavedSessionId { get; set; } = 0;
    
    public bool Wraps { get; set; } = false;
    public bool Underground { get; set; } = false;
    public bool Wilderness { get; set; } = false;
    public bool CombatEnabled { get; set; } = true;
    
    // Object Tracking
    public List<Object> Objects;
    
    // Object Layers - Multiple objects can exist at same location in different layers.
    private Dictionary<(int x, int y, ObjectLayer layer), Object> objectsByLocation;
    private Dictionary<(int x, int y), Place> subplacesByLocation = new();
    public List<(int x, int y, Place? destination)> Entrances { get; set; } = new();
    
    // Magic number for type checking (from Nazghul).
    public int Magic { get; set; } = 0x1234ABCD;
    
    public Place()
    {
        Width = Phantasma.Dimensions.MAP_TILE_W;
        Height = Phantasma.Dimensions.MAP_TILE_H;
        Name = "Test Map";
        TerrainGrid = new Terrain[Width, Height];
        objectsByLocation = new Dictionary<(int, int, ObjectLayer), Object>();
        Objects = new List<Object>(0);
        SetDefaultEdgeEntrances();
    }
    
    // Constructor for Scheme/Kernel (kern-mk-place)
    public Place(int width, int height, string name, bool wrapping, bool wilderness)
    {
        Width = width;
        Height = height;
        Name = name;
        Wraps = wrapping;
        // wilderness parameter stored later if needed
        TerrainGrid = new Terrain[Width, Height];
        objectsByLocation = new Dictionary<(int, int, ObjectLayer), Object>();
        Objects = new List<Object>();
        SetDefaultEdgeEntrances();
    }
    
    /// <summary>
    /// Gets the visibility/transparency value at a location.
    /// Returns 0 for opaque (blocks vision), 12 for transparent.
    /// </summary>
    public byte GetVisibility(int x, int y)
    {
        // TODO: Implement better way to return these values.
        var terrain = GetTerrain(x, y);
        if (terrain == null)
            return 12;
            
        return terrain.Transparent
            ? (byte)12
            : (byte)0;
    }
    
    /// <summary>
    /// Get terrain at a specific location.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <returns>Terrain at location, or null if out of bounds</returns>
    public Terrain? GetTerrain(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return null;
            
        return TerrainGrid[x, y];
    }
        
    /// <summary>
    /// Set terrain at a specific location.
    /// </summary>
    public void SetTerrain(int x, int y, Terrain terrain)
    {
        if (!IsInBounds(x, y))
            return;
            
        TerrainGrid[x, y] = terrain;
    }
    
    public void GenerateTestMap()
    {
        // Create some basic terrain types.
        var grass = new Terrain 
        { 
            Name = "grass", 
            DisplayChar = '.', 
            Color = "#228B22",  // Forest Green
            IsPassable = true 
        };
        
        var tree = new Terrain 
        { 
            Name = "tree", 
            DisplayChar = 'T', 
            Color = "#0F4F0F",  // Dark Green
            IsPassable = false,
            Transparent = false
        };
        
        var water = new Terrain 
        { 
            Name = "water", 
            DisplayChar = '~', 
            Color = "#4682B4",  // Steel Blue
            IsPassable = false,
            Transparent = true
        };
        
        var mountain = new Terrain 
        { 
            Name = "mountain", 
            DisplayChar = '^', 
            Color = "#808080",  // Gray
            IsPassable = false,
            Transparent = false
        };
        
        // Try to assign sprites if available.
        AssignSpriteToTerrain(grass, "grass");
        AssignSpriteToTerrain(tree, "tree");
        AssignSpriteToTerrain(water, "water");
        AssignSpriteToTerrain(mountain, "mountain");
        
        // Fill with grass.
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                TerrainGrid[x, y] = grass;
            }
        }
        
        // Add some trees.
        TerrainGrid[5, 5] = tree;
        TerrainGrid[6, 5] = tree;
        TerrainGrid[5, 6] = tree;
        TerrainGrid[15, 10] = tree;
        TerrainGrid[15, 11] = tree;
        TerrainGrid[16, 10] = tree;
        
        // Add a small lake.
        for (int y = 8; y < 12; y++)
        {
            for (int x = 2; x < 6; x++)
            {
                TerrainGrid[x, y] = water;
            }
        }
        
        // Add some mountains.
        TerrainGrid[18, 2] = mountain;
        TerrainGrid[19, 2] = mountain;
        TerrainGrid[18, 3] = mountain;
        TerrainGrid[19, 3] = mountain;
    }
    
    private void AssignSpriteToTerrain(Terrain terrain, string spriteTag)
    {
        var sprite = SpriteManager.GetSprite(spriteTag);
        if (sprite != null)
        {
            terrain.Sprite = sprite;
            System.Console.WriteLine($"Assigned sprite '{spriteTag}' to terrain '{terrain.Name}'.");
        }
        else
        {
            System.Console.WriteLine($"No sprite found for '{spriteTag}'; will use colored tile.");
        }
    }
    
    // ============================================================================
    // COORDINATE WRAPPING
    // ============================================================================
    
    /// <summary>
    /// Wraps X coordinate for maps that wrap around.
    /// </summary>
    public int WrapX(int x)
    {
        if (Wraps && Width > 0)
            return ((x % Width) + Width) % Width;
        return x;
    }
    
    /// <summary>
    /// Wraps Y coordinate for maps that wrap around.
    /// </summary>
    public int WrapY(int y)
    {
        if (Wraps && Height > 0)
            return ((y % Height) + Height) % Height;
        return y;
    }
    
    /// <summary>
    /// Check if coordinates are off the map.
    /// </summary>
    public bool IsOffMap(int x, int y)
    {
        if (Wraps)
            return false;
        return x < 0 || x >= Width || y < 0 || y >= Height;
    }

// ============================================================================
// SUBPLACE MANAGEMENT
// ============================================================================

    /// <summary>
    /// Get subplace at the given coordinates.
    /// </summary>
    public Place? GetSubplace(int x, int y)
    {
        // Wrap coordinates if needed
        x = WrapX(x);
        y = WrapY(y);
    
        if (subplacesByLocation.TryGetValue((x, y), out var subplace))
            return subplace;
    
        return null;
    }

    /// <summary>
    /// Add a subplace at the given coordinates.
    /// </summary>
    /// <returns>True if successful, false if failed</returns>
    public bool AddSubplace(Place subplace, int x, int y)
    {
        if (subplace == null)
            return false;
    
        // Check bounds.
        if (IsOffMap(x, y))
            return false;
    
        // Check if location already has a subplace.
        if (subplacesByLocation.ContainsKey((x, y)))
            return false;
    
        // Add to dictionary.
        subplacesByLocation[(x, y)] = subplace;
    
        // Set the subplace's parent location.
        subplace.Location = new Location(this, x, y);
    
        // Add to subplaces list.
        if (!Subplaces.Contains(subplace))
            Subplaces.Add(subplace);
    
        return true;
    }

    /// <summary>
    /// Remove a subplace from this place.
    /// </summary>
    public void RemoveSubplace(Place subplace)
    {
        if (subplace == null)
            return;
    
        var key = (subplace.Location.X, subplace.Location.Y);
    
        // Remove from dictionary if it matches.
        if (subplacesByLocation.TryGetValue(key, out var existing) && existing == subplace)
        {
            subplacesByLocation.Remove(key);
        }
    
        // Remove from subplaces list.
        Subplaces.Remove(subplace);
    
        // Clear parent reference.
        subplace.Location = new Location(null, 0, 0);
    }

    // ============================================================================
    // EDGE ENTRANCES
    // ============================================================================

    /// <summary>
    /// Set default edge entrances based on map dimensions.
    /// When entering from a direction, player appears on the opposite edge.
    /// </summary>
    public void SetDefaultEdgeEntrances()
    {
        int w = Width;
        int h = Height;
        
        // NEW BEHAVIOR: Enter from direction X → appear on edge X (same side)
        
        // Northwest: upper left corner (same corner you entered from)
        EdgeEntrances[Common.NORTHWEST, 0] = 0;
        EdgeEntrances[Common.NORTHWEST, 1] = 0;
        
        // North: upper center (north edge)
        EdgeEntrances[Common.NORTH, 0] = w / 2;
        EdgeEntrances[Common.NORTH, 1] = 0;
        
        // Northeast: upper right corner
        EdgeEntrances[Common.NORTHEAST, 0] = w - 1;
        EdgeEntrances[Common.NORTHEAST, 1] = 0;
        
        // West: left center
        EdgeEntrances[Common.WEST, 0] = 0;
        EdgeEntrances[Common.WEST, 1] = h / 2;
        
        // Here: center of map
        EdgeEntrances[Common.HERE, 0] = w / 2;
        EdgeEntrances[Common.HERE, 1] = h / 2;
        
        // East: right center
        EdgeEntrances[Common.EAST, 0] = w - 1;
        EdgeEntrances[Common.EAST, 1] = h / 2;
        
        // Southwest: lower left corner
        EdgeEntrances[Common.SOUTHWEST, 0] = 0;
        EdgeEntrances[Common.SOUTHWEST, 1] = h - 1;
        
        // South: lower center (south edge)
        EdgeEntrances[Common.SOUTH, 0] = w / 2;
        EdgeEntrances[Common.SOUTH, 1] = h - 1;
        
        // Southeast: lower right corner
        EdgeEntrances[Common.SOUTHEAST, 0] = w - 1;
        EdgeEntrances[Common.SOUTHEAST, 1] = h - 1;
    }

    /// <summary>
    /// Get the entrance coordinates for a given direction.
    /// </summary>
    /// <param name="dir">Direction entering from</param>
    /// <param name="x">Output X coordinate</param>
    /// <param name="y">Output Y coordinate</param>
    /// <returns>True if valid direction, false otherwise</returns>
    public bool GetEdgeEntrance(Direction dir, out int x, out int y)
    {
        int dirIndex = (int)dir;
        
        if (dirIndex < 0 || dirIndex >= 9)
        {
            x = 0;
            y = 0;
            return false;
        }
        
        x = EdgeEntrances[dirIndex, 0];
        y = EdgeEntrances[dirIndex, 1];
        return true;
    }

    /// <summary>
    /// Set the entrance coordinates for a given direction.
    /// </summary>
    /// <param name="dir">Direction entering from</param>
    /// <param name="x">X coordinate where player should appear</param>
    /// <param name="y">Y coordinate where player should appear</param>
    /// <returns>True if successful</returns>
    public bool SetEdgeEntrance(Direction dir, int x, int y)
    {
        int dirIndex = (int)dir;
        
        if (dirIndex < 0 || dirIndex >= 9)
            return false;
        
        // Validate coordinates are on map.
        if (IsOffMap(x, y))
            return false;
        
        EdgeEntrances[dirIndex, 0] = x;
        EdgeEntrances[dirIndex, 1] = y;
        return true;
    }

    // ============================================================================
    // PLACE ENTRY/EXIT
    // ============================================================================

    /// <summary>
    /// Called when a party/object enters this place.
    /// Synchronizes all objects to the current game time and executes
    /// the pre-entry hook if defined.
    /// </summary>
    public void Enter()
    {
        Console.WriteLine($"[Place.Enter] ===== ENTERING PLACE: {Name} =====");
        Console.WriteLine($"[Place.Enter] Object count: {Objects.Count}");
        
        // Synchronize all objects to current game time.
        // This positions scheduled NPCs at their correct locations.
        SynchronizeAllObjects();
    
        // Execute pre-entry hook if defined and callable.
        if (PreEntryHook is Callable callable)
        {
            Console.WriteLine($"[Place.Enter] Running pre-entry hook");
            callable.Call();
        }
    
        // Mark as dirty for rendering.
        Dirty = true;
        Console.WriteLine($"[Place.Enter] ===== ENTER COMPLETE =====");
    }

    /// <summary>
    /// Synchronize all objects in this place to the current game time.
    /// Called when entering a place to ensure NPCs are at their schedule locations.
    /// </summary>
    public void SynchronizeAllObjects()
    {
        var clock = Phantasma.MainSession?.Clock;
        Console.WriteLine($"[SynchronizeAllObjects] Current time: {clock?.Hour ?? -1}:{clock?.Min ?? -1:D2}");
        Console.WriteLine($"[SynchronizeAllObjects] Synchronizing {Objects.Count} objects...");
        
        int syncCount = 0;
        foreach (var obj in Objects.ToList())
        {
            // Check if this is a Character with a schedule.
            if (obj is Character ch)
            {
                Console.WriteLine($"[SynchronizeAllObjects] Found Character: {ch.GetName()}");
                Console.WriteLine($"[SynchronizeAllObjects]   Has Schedule: {ch.Schedule != null}");
                if (ch.Schedule != null)
                {
                    Console.WriteLine($"[SynchronizeAllObjects]   Schedule Tag: {ch.Schedule.Tag}");
                    Console.WriteLine($"[SynchronizeAllObjects]   Appointment Count: {ch.Schedule.Appointments.Count}");
                }
            }
            
            obj.Synchronize();
            syncCount++;
        }
        
        Console.WriteLine($"[SynchronizeAllObjects] Synchronized {syncCount} objects");
    }

    /// <summary>
    /// Explicit synchronization call - can be invoked from Scheme via kern-place-synch.
    /// </summary>
    public void Synchronize()
    {
        SynchronizeAllObjects();
    }

    /// <summary>
    /// Called when leaving this place.
    /// </summary>
    public void Exit()
    {
        // Currently just marks dirty - can be extended for cleanup.
        Dirty = true;
    }
    
    // ============================================================================
    // OBJECT MANAGEMENT
    // ============================================================================
    
    // Register object in place.
    public void RegisterObject(Object obj)
    {
        if (obj == null) return;
        
        if (!Objects.Contains(obj))
        {
            Objects.Add(obj);
        }
        
        // Add to layer lookup if object has valid position.
        if (obj.Position?.Place == this)
        {
            var key = (obj.GetX(), obj.GetY(), obj.Layer);
            objectsByLocation[key] = obj;
        }
    }
    
    /// <summary>
    /// Get object at a specific location and layer.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="layer">Object layer to check</param>
    /// <returns>Object at that layer, or null if none</returns>
    public Object? GetObjectAt(int x, int y, ObjectLayer layer)
    {
        if (!IsInBounds(x, y))
            return null;
        
        var key = (x, y, layer);
        return objectsByLocation.TryGetValue(key, out var obj) ? obj : null;
    }
    
    /// <summary>
    /// Get all objects at a specific location across all layers.
    /// Useful for rendering and interaction.
    /// </summary>
    public List<Object> GetObjectsAt(int x, int y)
    {
        var objects = new List<Object>();
        
        if (!IsInBounds(x, y))
            return objects;
        
        // Check each layer.
        foreach (ObjectLayer layer in Enum.GetValues(typeof(ObjectLayer)))
        {
            var obj = GetObjectAt(x, y, layer);
            if (obj != null)
                objects.Add(obj);
        }
        
        return objects;
    }
    
    /// <summary>
    /// Get the first object at (x,y) that matches the filter predicate.
    /// Returns null if no match found.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="filter">Predicate to test each object</param>
    /// <returns>First matching object or null</returns>
    public Object? GetFilteredObject(int x, int y, Func<Object, bool> filter)
    {
        // In Nazghul, this iterates through layers in order looking for match.
        // The objectsByLayer dictionary should work similarly.
    
        // Iterate through layers in rendering order (lowest to highest).
        var layerOrder = new[] {
            ObjectLayer.TerrainFeature,
            ObjectLayer.Mechanism,
            ObjectLayer.Portal,
            ObjectLayer.Vehicle,
            ObjectLayer.Bed,
            ObjectLayer.Container,
            ObjectLayer.Item,        // Most common for Get command
            ObjectLayer.Field,
            ObjectLayer.Being,
            ObjectLayer.Missile,
            ObjectLayer.Cursor
        };
    
        foreach (var layer in layerOrder)
        {
            if (!objectsByLocation.ContainsKey((x, y, layer)))
                continue;
            
            if (objectsByLocation.TryGetValue((x, y, layer), out var obj))
            {
                if (filter(obj))
                    return obj;
            }
        
            if (obj != null)
                return obj;
        }
    
        return null;
    }
    
    /// <summary>
    /// Place an object at a location on its appropriate layer.
    /// </summary>
    public void PlaceObject(Object obj, int x, int y)
    {
        if (!IsInBounds(x, y))
            return;
        
        // Determine layer based on object type.
        ObjectLayer layer = DetermineLayer(obj);
        
        var key = (x, y, layer);
        objectsByLocation[key] = obj;
        
        // Update object's location.
        if (obj.Position == null)
            obj.Position = new Location(this, x, y);
        else
        {
            obj.Position.Place = this;
            obj.Position.X = x;
            obj.Position.Y = y;
        }
    }
    /// <summary>
    /// Move an object from its current location to a new location.
    /// </summary>
    public void MoveObject(Object obj, int newX, int newY)
    {
        if (obj.Position == null)
        {
            // Object not placed yet, just place it.
            PlaceObject(obj, newX, newY);
            return;
        }
            
        // Remove from old location.
        ObjectLayer layer = DetermineLayer(obj);
        var oldKey = (obj.Position.X, obj.Position.Y, layer);
        objectsByLocation.Remove(oldKey);
            
        // Add to new location.
        var newKey = (newX, newY, layer);
        objectsByLocation[newKey] = obj;
            
        // Update object's location.
        obj.Position.X = newX;
        obj.Position.Y = newY;
    }
    
    public void AddObject(Object? obj, int x, int y)
    {
        if (obj == null || IsOffMap(x, y))
            return;
            
        obj.SetPosition(this, x, y);
        
        if (!Objects.Contains(obj))
        {
            Objects.Add(obj);
        }
        
        // Add to layer-based lookup dictionary for collision detection.
        var key = (x, y, obj.Layer);
        objectsByLocation[key] = obj;
    }
    
    public void RemoveObject(Object? obj)
    {
        if (obj != null)
        {
            Objects.Remove(obj);
            
            // Remove from dictionary as well.
            var key = (obj.GetX(), obj.GetY(), obj.Layer);
            objectsByLocation.Remove(key);
        }
    }
    
    public void MoveBeing(Being? being, int newX, int newY)
    {
        if (being == null || IsOffMap(newX, newY))
            return;
        
        // Remove from old location in dictionary.
        var oldKey = (being.GetX(), being.GetY(), ObjectLayer.Being);
        objectsByLocation.Remove(oldKey);
            
        // Update position.
        being.SetPosition(this, newX, newY);
        
        // Add to new location in dictionary.
        var newKey = (newX, newY, ObjectLayer.Being);
        objectsByLocation[newKey] = being;
    }
    
    public Being? GetBeingAt(int x, int y)
    {
        return Objects
            .OfType<Being>()
            .FirstOrDefault(b => b.GetX() == x && b.GetY() == y);
    }
    
    /// <summary>
    /// Get all vehicles in this place.
    /// </summary>
    public List<Vehicle> GetAllVehicles()
    {
        var vehicles = new List<Vehicle>();
    
        foreach (var kvp in objectsByLocation)
        {
            if (kvp.Key.layer == ObjectLayer.Vehicle && kvp.Value is Vehicle vehicle)
            {
                vehicles.Add(vehicle);
            }
        }
    
        return vehicles;
    }
    
    public List<Item> GetAllItems()
    {
        return Objects.OfType<Item>().ToList();
    }
    
    public List<Being> GetAllBeings()
    {
        return Objects.OfType<Being>().ToList();
    }

    public List<Missile> GetAllMissiles()
    {
        return Objects.OfType<Missile>().ToList();
    }
    
    public List<TerrainFeature> GetAllTerrainFeatures()
    {
        return Objects.OfType<TerrainFeature>().ToList();
    }
    
    public List<Mechanism> GetAllMechanisms()
    {
        return Objects.OfType<Mechanism>().ToList();
    }
    
    public List<Field> GetAllFields()
    {
        return Objects.OfType<Field>().ToList();
    }
    
    public Object? GetMechanismAt(int x, int y)
    {
        return Objects
            .FirstOrDefault(o => o.Layer == ObjectLayer.Mechanism && 
                                 o.GetX() == x && o.GetY() == y);
    }
    
    public bool IsPassable(int x, int y, Object forObject, int flags)
    {
        if (IsOffMap(x, y))
            return false;
            
        var terrain = GetTerrain(x, y);
        if (terrain != null)
        {
            var ptable = Phantasma.GetRegisteredObject("ptable") as PassabilityTable 
                         ?? PassabilityTable.CreateDefault();
            int movementMode = (forObject is Character ch) ? ch.Species.MovementMode.Index : 0;
            if (!ptable.IsPassable(movementMode, terrain.PassabilityClass))
                return false;
        }
            
        // Check for blocking objects.
        var objectsHere = GetObjectsAt(x, y);
        foreach (var obj in objectsHere)
        {
            if (obj != forObject && obj.Layer == ObjectLayer.Being)
                return false;
        }
        
        return true;
    }
    
    public int GetFlyingDistance(int x1, int y1, int x2, int y2)
    {
        int dx = System.Math.Abs(x2 - x1);
        int dy = System.Math.Abs(y2 - y1);
        return System.Math.Max(dx, dy); // Chebyshev distance
    }
    
    public bool IsInLineOfSight(int x1, int y1, int x2, int y2)
    {
        // Simple LOS
        return true;
    }

    /// <summary>
    /// Check if a location is passable for movement.
    /// </summary>
    /// <param name="x">X coordinate in map</param>
    /// <param name="y">Y coordinate in map</param>
    /// <param name="subject">Object trying to move (for future vehicle/mode checks).</param>
    /// <param name="checkBeings">If true, also check for beings blocking the space.</param>
    /// <param name="checkMechanisms">If true, check for blocking mechanisms (doors, etc.).</param>
    /// <returns>True if the location can be entered, false if blocked.</returns>
    public bool IsPassable(int x, int y, Object? subject = null, 
                           bool checkBeings = true, bool checkMechanisms = true, bool isMovementAttempt = false)
    {
        // Check map boundaries.
        if (!IsInBounds(x, y))
            return false;
        
        const int PCLASS_NONE = 0;
        const int IGNORES_PASSABILITY = 0;
        const int ALLOWS_PASSABILITY = 1;
        const int BLOCKS_PASSABILITY = -1;
        
        int tfeatPass = IGNORES_PASSABILITY;
        
        // =====================================================
        // STEP 1: Check terrain features (bridges, etc.)
        // Terrain features can OVERRIDE base terrain passability.
        // =====================================================
        var tfeat = GetObjectAt(x, y, ObjectLayer.TerrainFeature);
        if (tfeat != null)
        {
            int pclass = tfeat.PassabilityClass;
            
            // Does the object care about passability?
            if (pclass == PCLASS_NONE)
            {
                tfeatPass = IGNORES_PASSABILITY;
            }
            else
            {
                // Check if subject can pass this pclass.
                var ptable = Phantasma.GetRegisteredObject("ptable") as PassabilityTable 
                             ?? PassabilityTable.CreateDefault();
                int movementMode = 0;
                if (subject is Character ch)
                    movementMode = ch.Species.MovementMode.Index;
                
                if (ptable.IsPassable(movementMode, pclass))
                {
                    tfeatPass = ALLOWS_PASSABILITY;
                }
                else
                {
                    tfeatPass = BLOCKS_PASSABILITY;
                }
            }
            
            // If terrain feature specifically BLOCKS, return false.
            if (tfeatPass == BLOCKS_PASSABILITY)
            {
                return false;
            }
        }
        
        // =====================================================
        // STEP 2: Check base terrain ONLY if tfeat ignores passability
        // =====================================================
        if (tfeatPass == IGNORES_PASSABILITY)
        {
            var terrain = GetTerrain(x, y);
            if (terrain != null)
            {
                var ptable = Phantasma.GetRegisteredObject("ptable") as PassabilityTable 
                             ?? PassabilityTable.CreateDefault();
                int movementMode = 0;
                if (subject is Character ch)
                    movementMode = ch.Species.MovementMode.Index;
                
                if (!ptable.IsPassable(movementMode, terrain.PassabilityClass))
                {
                    return false;
                }
            }
        }
        // If tfeatPass == ALLOWS_PASSABILITY, we SKIP terrain check (bridge over water).
        
        // =====================================================
        // STEP 3: Check for beings occupying the space
        // =====================================================
        if (checkBeings)
        {
            if (IsOccupied(x, y))
                return false;
        }
        
        // =====================================================
        // STEP 4: Check for blocking mechanisms (doors, etc.).
        // =====================================================
        if (checkMechanisms)
        {
            var mechanism = GetObjectAt(x, y, ObjectLayer.Mechanism);
            
            if (mechanism != null)
            {
                int mechPclass = mechanism.PassabilityClass;
                
                // Does the object care about passability?
                // PCLASS_NONE (0) means ignore passability.
                if (mechPclass != PCLASS_NONE)
                {
                    // Check if subject can pass this pclass.
                    var ptable = Phantasma.GetRegisteredObject("ptable") as PassabilityTable 
                                 ?? PassabilityTable.CreateDefault();
                    int movementMode = 0;
                    if (subject is Character ch)
                        movementMode = ch.Species.MovementMode.Index;
                    
                    if (!ptable.IsPassable(movementMode, mechPclass))
                    {
                        // =========================================================
                        // BUMP HANDLING: Try to open doors on movement attempt.
                        // =========================================================
                        
                        if (isMovementAttempt)
                        {
                            var objType = mechanism.Type;
                            
                            if (objType?.CanBump == true)
                            {
                                var gifc = objType.InteractionHandler;
                                if (gifc is Callable callable)
                                {
                                    try
                                    {
                                        // Nazghul's bump() sends 'open, not 'bump!
                                        var openSymbol = SymbolTable.StringToObject("open");
                                        callable.Call(openSymbol, mechanism, subject);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[Bump] Error: {ex.Message}");
                                    }
                                }
                            }
                        }
                        
                        return false;
                    }
                }
            }
        }
        
        return true;
    }
        
    /// <summary>
    /// Determine which layer an object belongs to.
    /// </summary>
    private ObjectLayer DetermineLayer(Object obj)
    {
        // First check if object has an explicit layer from its type.
        if (obj.Type?.Layer != null && obj.Type.Layer != ObjectLayer.Null)
            return obj.Type.Layer;
        
        // Also check if object itself has a Layer property.
        if (obj.Layer != ObjectLayer.Null)
            return obj.Layer;
        
        // For now, determine by type.
        if (obj is Being)
            return ObjectLayer.Being;
        else if (obj is IMechanism)
            return ObjectLayer.Mechanism;
        else if (obj is Portal)
            return ObjectLayer.Portal;
        else if (obj is Container)
            return ObjectLayer.Container;
        else if (obj.GetType().Name.Contains("Feature"))
            return ObjectLayer.TerrainFeature;
        else if (obj.GetType().Name.Contains("Field"))
            return ObjectLayer.Field;
        else
            return ObjectLayer.Item;
    }
    
    /// <summary>
    /// Check if coordinates are within valid map bounds.
    /// </summary>
    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }
    
    /// <summary>
    /// Check if a location is occupied by a being.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <returns>True if a being is at this location.</returns>
    public bool IsOccupied(int x, int y)
    {
        if (!IsInBounds(x, y))
            return false;
        
        return GetObjectAt(x, y, ObjectLayer.Being) != null;
    }
    
    /// <summary>
    /// Get the movement cost for entering this location.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="subject">Object moving (for future vehicle/mode considerations).</param>
    /// <returns>Movement cost (1.0 = normal, higher = slower). MaxValue if impassable.</returns>
    public int GetMovementCost(int x, int y, Object? subject = null)
    {
        if (!IsInBounds(x, y))
            return 255; // Impassable
        
        var ptable = Phantasma.GetRegisteredObject("ptable") as PassabilityTable 
                     ?? PassabilityTable.CreateDefault();
        int movementMode = 0;
        if (subject is Character ch)
            movementMode = ch.Species.MovementMode.Index;
        
        // Check terrain feature first (bridges override water cost).
        var tfeat = GetObjectAt(x, y, ObjectLayer.TerrainFeature);
        if (tfeat != null && tfeat.PassabilityClass != 0)
        {
            return ptable.GetCost(movementMode, tfeat.PassabilityClass);
        }
        
        // Fall back to terrain.
        var terrain = GetTerrain(x, y);
        if (terrain != null)
        {
            return ptable.GetCost(movementMode, terrain.PassabilityClass);
        }
        
        return 1; // Default cost
    }
    
    /// <summary>
    /// Check if a location is hazardous (causes damage or effects).
    /// Based on Nazghul's terrain hazard system.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <returns>True if this location is hazardous.</returns>
    public bool IsHazardous(int x, int y)
    {
        if (!IsInBounds(x, y))
            return false;
        
        var terrain = GetTerrain(x, y);
        return terrain?.IsHazardous ?? false;
    }
    
    /// <summary>
    /// Check if movement from one location to another is valid.
    /// Useful for diagonal movement checks.
    /// </summary>
    /// <param name="fromX">Starting X</param>
    /// <param name="fromY">Starting Y</param>
    /// <param name="toX">Destination X</param>
    /// <param name="toY">Destination Y</param>
    /// <param name="subject">Object moving</param>
    /// <returns>True if movement is valid</returns>
    public bool CanMoveTo(int fromX, int fromY, int toX, int toY, Object? subject = null)
    {
        // Check if destination is passable.
        if (!IsPassable(toX, toY, subject))
            return false;
        
        // For diagonal movement, check if the path is clear.
        // (Can't squeeze through diagonal gaps.)
        int dx = toX - fromX;
        int dy = toY - fromY;
        
        if (dx != 0 && dy != 0) // Diagonal move
        {
            // Check both cardinal directions that form this diagonal.
            bool horizontalClear = IsPassable(fromX + dx, fromY, subject, false, false);
            bool verticalClear = IsPassable(fromX, fromY + dy, subject, false, false);
            
            // Need at least one clear path for diagonal movement.
            if (!horizontalClear && !verticalClear)
                return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Get a description of why a location is blocked.
    /// Useful for player feedback.
    /// </summary>
    public string GetBlockageReason(int x, int y)
    {
        if (!IsInBounds(x, y))
            return "Out of bounds";
        
        var terrain = GetTerrain(x, y);
        if (terrain != null)
        {
            var ptable = Phantasma.GetRegisteredObject("ptable") as PassabilityTable 
                         ?? PassabilityTable.CreateDefault();
            if (!ptable.IsPassable(0, terrain.PassabilityClass))  // 0 = walking
                return $"Blocked by {terrain.Name}";
        }
        
        if (IsOccupied(x, y))
        {
            var being = GetObjectAt(x, y, ObjectLayer.Being) as Being;
            return $"Occupied by {being?.Name ?? "someone"}";
        }
        
        var mechanism = GetObjectAt(x, y, ObjectLayer.Mechanism);
        if (mechanism != null && mechanism is IBlockingObject blocker && !blocker.IsPassable)
            return $"Blocked by {mechanism.Name}";
        
        return "Unknown obstruction";
    }
    
    public IEnumerable<Portal> GetAllPortals()
    {
        return objectsByLocation
            .Where(kvp => kvp.Key.layer == ObjectLayer.Portal)
            .Select(kvp => kvp.Value)
            .OfType<Portal>();
    }

// ============================================================================
// HELPER METHODS
// ============================================================================
/*
    /// <summary>
    /// Get tile at coordinates, or null if none exists.
    /// </summary>
    private Tile? GetTileAt(int x, int y)
    {
        if (tiles.TryGetValue((x, y), out var tile))
            return tile;
        return null;
    }
    
    /// <summary>
    /// Get or create tile at coordinates.
    /// </summary>
    private Tile GetOrCreateTileAt(int x, int y)
    {
        if (!tiles.TryGetValue((x, y), out var tile))
        {
            tile = new Tile();
            tiles[(x, y)] = tile;
        }
        return tile;
    }*/
}
