using System.Collections.Generic;
using System.Linq;

namespace Phantasma.Models;

public class Place
{
    public Terrain[,] TerrainGrid { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Name { get; set; }
        
    // Object Tracking
    private List<Object> objects;

    // Magic number for type checking (from Nazghul).
    public int Magic { get; set; } = 0x1234ABCD;

    public Place()
    {
        Width = 20;  // TODO: Change to Dimensions.MAP_TILE_W after making Dimensions static.
        Height = 20; // TODO: Chamge to Dimensions.MAP_TILE_H after making Dimensions static.
        Name = "Test Map";
        TerrainGrid = new Terrain[Width, Height];
        objects = new List<Object>();

    }

    public void GenerateTestMap()
    {
        // Create some basic terrain types.
        var grass = new Terrain 
        { 
            Name = "grass", 
            DisplayChar = '.', 
            Color = "#228B22",  // Forest Green
            Passable = true 
        };
        
        var tree = new Terrain 
        { 
            Name = "tree", 
            DisplayChar = 'T', 
            Color = "#0F4F0F",  // Dark Green
            Passable = false 
        };
        
        var water = new Terrain 
        { 
            Name = "water", 
            DisplayChar = '~', 
            Color = "#4682B4",  // Steel Blue
            Passable = false 
        };
        
        var mountain = new Terrain 
        { 
            Name = "mountain", 
            DisplayChar = '^', 
            Color = "#808080",  // Gray
            Passable = false 
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

    public Terrain GetTerrainAt(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return null;
            
        return TerrainGrid[x, y];
    }

    public bool IsOffMap(int x, int y)
    {
        return x < 0 || x >= Width || y < 0 || y >= Height;
    }
    
    // Object Management Methods
    
    public void AddObject(Object obj, int x, int y)
    {
        if (obj == null || IsOffMap(x, y))
            return;
            
        obj.SetPosition(this, x, y);
        
        if (!objects.Contains(obj))
        {
            objects.Add(obj);
        }
    }
    
    public void RemoveObject(Object obj)
    {
        if (obj != null)
        {
            objects.Remove(obj);
        }
    }
    
    public void MoveBeing(Being being, int newX, int newY)
    {
        if (being == null || IsOffMap(newX, newY))
            return;
            
        // Just update position - being is already in objects list.
        being.SetPosition(this, newX, newY);
    }
    
    public Being GetBeingAt(int x, int y)
    {
        return objects
            .OfType<Being>()
            .FirstOrDefault(b => b.GetX() == x && b.GetY() == y);
    }
    
    public List<Object> GetObjectsAt(int x, int y)
    {
        return objects
            .Where(o => o.GetX() == x && o.GetY() == y)
            .OrderBy(o => o.ObjectLayer)  // Sort by layer for proper drawing order.
            .ToList();
    }
    
    public List<Being> GetAllBeings()
    {
        return objects.OfType<Being>().ToList();
    }
    
    public Object GetMechanismAt(int x, int y)
    {
        return objects
            .FirstOrDefault(o => o.ObjectLayer == Object.Layer.Mechanism && 
                               o.GetX() == x && o.GetY() == y);
    }
    
    public bool IsPassable(int x, int y, Object forObject, int flags)
    {
        if (IsOffMap(x, y))
            return false;
            
        var terrain = GetTerrainAt(x, y);
        if (terrain != null && !terrain.Passable)
            return false;
            
        // Check for blocking objects.
        var objectsHere = GetObjectsAt(x, y);
        foreach (var obj in objectsHere)
        {
            if (obj != forObject && obj.ObjectLayer == Object.Layer.Being)
                return false;
        }
        
        return true;
    }
    
    public int GetMovementCost(int x, int y, Object forObject)
    {
        if (!IsPassable(x, y, forObject, 0))
            return int.MaxValue;
            
        return 1; // Simple cost for now
    }
    
    public bool IsHazardous(int x, int y)
    {
        var terrain = GetTerrainAt(x, y);
        return terrain != null && terrain.IsHazardous();
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
}