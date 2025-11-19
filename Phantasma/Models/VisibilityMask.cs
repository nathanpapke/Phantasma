using System;
using System.Collections.Generic;

namespace Phantasma.Models;

public class VisibilityMask
{
    private const int VmaskWidth = 39;  // MAP_TILE_W * 2 + 1 = 19*2+1
    private const int VmaskHeight = 39;
    private const int VmaskSize = VmaskWidth * VmaskHeight;
    private const int HighWaterMark = 100;
    private const int LowWaterMark = 50;
    
    private Dictionary<string, VisibilityMaskEntry> _cache;
    private LinkedList<VisibilityMaskEntry> _lruQueue;
    private LineOfSight _losEngine;
    
    /// <summary>
    /// Internal cache entry containing the visibility mask data
    /// </summary>
    private class VisibilityMaskEntry
    {
        public string Key { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public LinkedListNode<VisibilityMaskEntry>? Node { get; set; }
    }
    
    /// <summary>
    /// Creates a new visibility mask cache
    /// </summary>
    public VisibilityMask()
    {
        _cache = new Dictionary<string, VisibilityMaskEntry>();
        _lruQueue = new LinkedList<VisibilityMaskEntry>();
        _losEngine = new LineOfSight(VmaskWidth, VmaskHeight, VmaskWidth / 2);
    }
    
    /// <summary>
    /// Gets the visibility mask for a location, computing it if necessary.
    /// The returned array is valid until the next call to Get().
    /// </summary>
    /// <param name="place">The place/map</param>
    /// <param name="x">X coordinate (center of visibility)</param>
    /// <param name="y">Y coordinate (center of visibility)</param>
    /// <returns>Visibility mask array</returns>
    public byte[] Get(Place place, int x, int y)
    {
        x = place.WrapX(x);
        y = place.WrapY(y);
        
        string key = MakeKey(place, x, y);
        
        if (_cache.TryGetValue(key, out var entry))
        {
            // Move to front of LRU queue (most recently used).
            if (entry.Node != null)
            {
                _lruQueue.Remove(entry.Node);
                entry.Node = _lruQueue.AddFirst(entry);
            }
            return entry.Data;
        }
        
        // Create new visibility mask.
        return CreateVisibilityMask(key, place, x, y);
    }
    
    /// <summary>
    /// Creates a new visibility mask by computing line of sight.
    /// </summary>
    private byte[] CreateVisibilityMask(string key, Place place, int centerX, int centerY)
    {
        // Purge old entries if needed.
        if (_cache.Count >= HighWaterMark)
        {
            Purge();
        }
        
        // Build alpha mask (transparency) from surrounding terrain.
        int startX = centerX - VmaskWidth / 2;
        int startY = centerY - VmaskHeight / 2;
        
        int index = 0;
        for (int y = 0; y < VmaskHeight; y++)
        {
            for (int x = 0; x < VmaskWidth; x++)
            {
                int mapX = place.WrapX(startX + x);
                int mapY = place.WrapY(startY + y);
                
                _losEngine.Alpha[index] = place.GetVisibility(mapX, mapY);
                index++;
            }
        }
        
        // Compute line of sight.
        _losEngine.Compute();
        
        // Cache the result.
        var data = new byte[VmaskSize];
        Array.Copy(_losEngine.VisibilityMask, data, VmaskSize);
        
        var entry = new VisibilityMaskEntry
        {
            Key = key,
            Data = data
        };
        
        entry.Node = _lruQueue.AddFirst(entry);
        _cache[key] = entry;
        
        return data;
    }
    
    /// <summary>
    /// Invalidates cached visibility masks in an area.
    /// Call this when terrain changes (e.g., door opens, wall destroyed).
    /// </summary>
    /// <param name="place">The place/map</param>
    /// <param name="x">X coordinate of changed area</param>
    /// <param name="y">Y coordinate of changed area</param>
    /// <param name="width">Width of changed area (1 for single tile)</param>
    /// <param name="height">Height of changed area (1 for single tile)</param>
    public void Invalidate(Place place, int x, int y, int width, int height)
    {
        // Invalidate vmasks in the affected area
        int startX = x - VmaskWidth / 2;
        int startY = y - VmaskHeight / 2;
        int endX = startX + width + VmaskWidth;
        int endY = startY + height + VmaskHeight;
        
        var toRemove = new List<string>();
        
        for (int py = startY; py < endY; py++)
        {
            for (int px = startX; px < endX; px++)
            {
                string key = MakeKey(place, px, py);
                if (_cache.ContainsKey(key))
                {
                    toRemove.Add(key);
                }
            }
        }
        
        foreach (var key in toRemove)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.Node != null)
                {
                    _lruQueue.Remove(entry.Node);
                }
                _cache.Remove(key);
            }
        }
    }
    
    /// <summary>
    /// Invalidates all cached visibility masks everywhere.
    /// Use when changing places or for debugging.
    /// </summary>
    public void InvalidateAll()
    {
        _cache.Clear();
        _lruQueue.Clear();
    }
    
    /// <summary>
    /// Purges least recently used entries until cache size is reasonable.
    /// </summary>
    private void Purge()
    {
        while (_cache.Count > LowWaterMark)
        {
            var last = _lruQueue.Last!;
            _lruQueue.RemoveLast();
            _cache.Remove(last.Value.Key);
        }
    }

    /// <summary>
    /// Creates a unique cache key for a location.
    /// </summary>
    private string MakeKey(Place place, int x, int y)
    {
        return $"{x}:{y}:{place.Name}";
    }


    /// <summary>
    /// Gets the current cache size (for debugging/monitoring).
    /// </summary>
    public int CacheSize => _cache.Count;
}