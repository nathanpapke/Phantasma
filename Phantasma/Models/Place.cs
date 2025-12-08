using System;
using System.Collections.Generic;
using System.Linq;

namespace Phantasma.Models;

public class Place
{
    public string Tag { get; set; } = "";
    public string Name { get; set; }
    public Sprite? Sprite { get; set; }
    public Terrain[,] TerrainGrid { get; set; }
    public bool Wraps { get; set; } = false;
    public bool IsUnderground { get; set; } = false;
    public bool IsWilderness { get; set; } = false;
    public bool CombatEnabled { get; set; } = true;
    public List<Place> SubPlaces { get; set; } = new();
    public Dictionary<string, Place> Neighbors { get; set; } = new();
        
    // Object Tracking
    public List<Object> Objects;
    
    // Object Layers - Multiple objects can exist at same location in different layers.
    private Dictionary<(int x, int y, ObjectLayer layer), Object> objectsByLocation;
    
    public List<(int x, int y, Place? destination)> Entrances { get; set; } = new();
    
    public int Width { get; set; }
    public int Height { get; set; }

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
    }
    
    /// <summary>
    /// Wraps X coordinate for maps that wrap around
    /// </summary>
    public int WrapX(int x)
    {
        if (!Wraps) return x;
        
        while (x < 0) x += Width;
        while (x >= Width) x -= Width;
        return x;
    }
    
    /// <summary>
    /// Wraps Y coordinate for maps that wrap around
    /// </summary>
    public int WrapY(int y)
    {
        if (!Wraps) return y;
        
        while (y < 0) y += Height;
        while (y >= Height) y -= Height;
        return y;
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

    public bool IsOffMap(int x, int y)
    {
        return x < 0 || x >= Width || y < 0 || y >= Height;
    }
    
    // Object Management Methods
    
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
    
    public List<Item> GetAllItems()
    {
        return Objects.OfType<Item>().ToList();
    }
    
    public List<Being> GetAllBeings()
    {
        return Objects.OfType<Being>().ToList();
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
        if (terrain != null && !terrain.IsPassable)
            return false;
            
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
                           bool checkBeings = true, bool checkMechanisms = true)
    {
        // Check map boundaries.
        // Nazghul: "For a non-wrapping place, return impassable."
        if (!IsInBounds(x, y))
            return false;
        
        // Check terrain passability.
        var terrain = GetTerrain(x, y);
        if (terrain != null && !terrain.IsPassable)
        {
            // Future: Check if subject has vehicle that makes this passable.
            // For now, terrain passability is absolute.
            return false;
        }
        
        // Check for beings occupying the space.
        if (checkBeings)
        {
            if (IsOccupied(x, y))
                return false; // Can't walk through other beings.
        }
        
        // Check for blocking mechanisms (doors, containers, etc.).
        if (checkMechanisms)
        {
            var mechanism = GetObjectAt(x, y, ObjectLayer.Mechanism);
            if (mechanism != null && mechanism is IBlockingObject blocker)
            {
                if (!blocker.IsPassable)
                    return false; // Blocked by closed door, etc.
            }
        }
        
        return true;
    }
        
    /// <summary>
    /// Determine which layer an object belongs to.
    /// </summary>
    private ObjectLayer DetermineLayer(Object obj)
    {
        // In Nazghul, objects have getLayer() method.
        // For now, determine by type.
        if (obj is Being)
            return ObjectLayer.Being;
        else if (obj is IMechanism)
            return ObjectLayer.Mechanism;
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
    public float GetMovementCost(int x, int y, Object? subject = null)
    {
        if (!IsInBounds(x, y))
            return float.MaxValue; // Out of bounds = infinite cost
        
        var terrain = GetTerrain(x, y);
        if (terrain == null)
            return 1.0f; // Default cost if no terrain
        
        if (!terrain.IsPassable)
            return float.MaxValue; // Impassable = infinite cost
        
        // Future: Factor in subject's movement mode and vehicle.
        // For now, just return terrain's base cost.
        return terrain.MovementCost;
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
        if (terrain != null && !terrain.IsPassable)
            return $"Blocked by {terrain.Name}";
        
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
}
