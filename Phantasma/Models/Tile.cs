using System;
using System.Collections.Generic;

namespace Phantasma.Models;

/// <summary>
/// Represents a single tile location with multiple object layers.
/// </summary>
/// <remarks>
/// In Nazghul, tiles are created on-demand when objects are placed.
/// Each tile maintains a stack of objects organized by layer.
/// Objects at lower layers render first, higher layers on top.
/// </remarks>
public class Tile
{
    /// <summary>
    /// X Coordinate of this Tile
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Y Coordinate of this Tile
    /// </summary>
    public int Y { get; }

    /// <summary>
    /// Objects Organized by Layer
    /// Each layer can have multiple objects (e.g., stacked items).
    /// </summary>
    private Dictionary<ObjectLayer, List<Object>> objectsByLayer;

    /// <summary>
    /// Total count of objects on this tile.
    /// </summary>
    public int ObjectCount { get; private set; }

    /// <summary>
    /// Create a new tile at the specified coordinates.
    /// </summary>
    public Tile(int x, int y)
    {
        X = x;
        Y = y;
        objectsByLayer = new Dictionary<ObjectLayer, List<Object>>();
        ObjectCount = 0;
    }

    /// <summary>
    /// Get the topmost object at a specific layer.
    /// </summary>
    /// <param name="layer">Layer to query</param>
    /// <returns>Top object at that layer, or null if none</returns>
    public Object? GetObjectAtLayer(ObjectLayer layer)
    {
        if (!objectsByLayer.TryGetValue(layer, out var objects))
            return null;
        
        // Return the last added (topmost) object at this layer.
        return objects.Count > 0 ? objects[^1] : null;
    }

    /// <summary>
    /// Get ALL objects at a specific layer.
    /// Useful for inventory systems where multiple items stack.
    /// </summary>
    /// <param name="layer">Layer to query</param>
    /// <returns>List of all objects at that layer</returns>
    public List<Object> GetObjectsAtLayer(ObjectLayer layer)
    {
        if (!objectsByLayer.TryGetValue(layer, out var objects))
            return new List<Object>();
        
        // Return a copy to prevent external modification.
        return new List<Object>(objects);
    }

    /// <summary>
    /// Get all objects across all layers (unordered).
    /// </summary>
    /// <returns>List of all objects on this tile</returns>
    public List<Object> GetAllObjects()
    {
        var allObjects = new List<Object>();
        
        foreach (var layerObjects in objectsByLayer.Values)
        {
            allObjects.AddRange(layerObjects);
        }
        
        return allObjects;
    }

    /// <summary>
    /// Get all objects in rendering order (layer order, bottom to top).
    /// Lower layer numbers render first, higher on top.
    /// </summary>
    /// <returns>Objects sorted by layer for rendering</returns>
    public List<Object> GetObjectsInRenderOrder()
    {
        var allObjects = GetAllObjects();
        
        // Sort by layer: ascending order means bottom to top.
        allObjects.Sort((a, b) => a.Layer.CompareTo(b.Layer));
        
        return allObjects;
    }

    /// <summary>
    /// Add an object to this tile.
    /// </summary>
    /// <param name="obj">Object to add</param>
    public void AddObject(Object obj)
    {
        var layer = obj.Layer;
        
        // Create layer list if it doesn't exist.
        if (!objectsByLayer.ContainsKey(layer))
        {
            objectsByLayer[layer] = new List<Object>();
        }
        
        // Add object to layer.
        objectsByLayer[layer].Add(obj);
        ObjectCount++;
    }

    /// <summary>
    /// Remove an object from this tile.
    /// </summary>
    /// <param name="obj">Object to remove</param>
    /// <returns>True if object was removed, false if not found</returns>
    public bool RemoveObject(Object obj)
    {
        var layer = obj.Layer;
        
        if (!objectsByLayer.TryGetValue(layer, out var objects))
            return false;
        
        bool removed = objects.Remove(obj);
        
        if (removed)
        {
            ObjectCount--;
            
            // Clean up empty layer lists to save memory.
            if (objects.Count == 0)
            {
                objectsByLayer.Remove(layer);
            }
        }
        
        return removed;
    }

    /// <summary>
    /// Check if there are any objects at a specific layer.
    /// </summary>
    /// <param name="layer">Layer to check</param>
    /// <returns>True if layer has objects</returns>
    public bool HasObjectsAtLayer(ObjectLayer layer)
    {
        return objectsByLayer.ContainsKey(layer) && 
               objectsByLayer[layer].Count > 0;
    }

    /// <summary>
    /// Check if tile has any objects at all.
    /// </summary>
    public bool IsEmpty => ObjectCount == 0;

    /// <summary>
    /// Execute a function for each object on this tile.
    /// </summary>
    /// <param name="action">Action to execute for each object</param>
    public void ForEachObject(Action<Object> action)
    {
        // Iterate through all layers.
        foreach (var layerObjects in objectsByLayer.Values)
        {
            // Iterate through objects in layer.
            // Make a copy to allow modifications during iteration.
            var objectsCopy = new List<Object>(layerObjects);
            foreach (var obj in objectsCopy)
            {
                action(obj);
            }
        }
    }

    /// <summary>
    /// Get a filtered object from this tile.
    /// </summary>
    /// <param name="filter">Predicate to test objects</param>
    /// <returns>First object matching filter, or null</returns>
    public Object? GetFilteredObject(Predicate<Object> filter)
    {
        // Traverse in reverse order (topmost first).
        // This matches Nazghul's behavior.
        var allObjects = GetAllObjects();
        allObjects.Reverse();
        
        foreach (var obj in allObjects)
        {
            if (filter(obj))
                return obj;
        }
        
        return null;
    }

    /// <summary>
    /// Get count of objects at a specific layer.
    /// </summary>
    /// <param name="layer">Layer to count</param>
    /// <returns>Number of objects at that layer</returns>
    public int GetLayerCount(ObjectLayer layer)
    {
        if (!objectsByLayer.TryGetValue(layer, out var objects))
            return 0;
        
        return objects.Count;
    }

    /// <summary>
    /// Get all layers that have objects.
    /// </summary>
    /// <returns>Set of layers with objects</returns>
    public IEnumerable<ObjectLayer> GetActiveLayers()
    {
        return objectsByLayer.Keys;
    }

    public override string ToString()
    {
        return $"Tile({X},{Y}): {ObjectCount} objects across {objectsByLayer.Count} layers";
    }
}