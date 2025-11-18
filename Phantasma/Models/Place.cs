namespace Phantasma.Models;

public class Place
{
    public Terrain[,] TerrainGrid { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Name { get; set; }

    // Magic number for type checking (from Nazghul).
    public int Magic { get; set; } = 0x1234ABCD;

    public Place()
    {
        Width = 20;
        Height = 20;
        Name = "Test Map";
        TerrainGrid = new Terrain[Width, Height];
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
}