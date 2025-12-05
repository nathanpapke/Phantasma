namespace Phantasma.Models;

/// <summary>
/// Passability table for advanced movement mode support.
/// </summary>
/// <remarks>
/// This enables different movement modes (walking, swimming, flying)
/// to have different costs for different terrain types.
/// 
/// Example: Water terrain
/// - Walking mode: IMPASSABLE (can't walk on water)
/// - Swimming mode: 1 (normal speed in water)
/// - Flying mode: 1 (can fly over water)
/// - Sailing mode: 1 (boat moves on water)
/// 
/// This is currently a skeleton implementation. Full integration will
/// happen in a later task when we add vehicles and movement modes.
/// </remarks>
public class PassabilityTable
{
    /// <summary>
    /// Special value indicating terrain is impassable for this mode.
    /// </summary>
    public const int IMPASSABLE = 255;

    /// <summary>
    /// Default passability class for undefined terrain.
    /// </summary>
    public const int PCLASS_NONE = 0;

    private int numMovementModes;
    private int numPassabilityClasses;
    private int[,] costTable;

    /// <summary>
    /// Create a new passability table.
    /// </summary>
    /// <param name="movementModes">Number of movement modes</param>
    /// <param name="passabilityClasses">Number of terrain passability classes</param>
    public PassabilityTable(int movementModes, int passabilityClasses)
    {
        numMovementModes = movementModes;
        numPassabilityClasses = passabilityClasses;
        costTable = new int[movementModes, passabilityClasses];
        
        // Initialize all to passable with normal cost.
        for (int m = 0; m < movementModes; m++)
        {
            for (int p = 0; p < passabilityClasses; p++)
            {
                costTable[m, p] = 1; // Default: cost of 1
            }
        }
    }

    /// <summary>
    /// Set movement cost for a mode/class combination.
    /// </summary>
    /// <param name="movementMode">Movement mode index</param>
    /// <param name="passabilityClass">Terrain passability class</param>
    /// <param name="cost">Movement cost (1=normal, higher=slower, IMPASSABLE=blocked)</param>
    public void SetCost(int movementMode, int passabilityClass, int cost)
    {
        if (IsValidIndex(movementMode, passabilityClass))
        {
            costTable[movementMode, passabilityClass] = cost;
        }
    }

    /// <summary>
    /// Get movement cost for a mode/class combination.
    /// </summary>
    /// <returns>Movement cost, or IMPASSABLE if invalid</returns>
    public int GetCost(int movementMode, int passabilityClass)
    {
        if (IsValidIndex(movementMode, passabilityClass))
        {
            return costTable[movementMode, passabilityClass];
        }
        return IMPASSABLE;
    }

    /// <summary>
    /// Check if a mode/class combination is passable.
    /// </summary>
    public bool IsPassable(int movementMode, int passabilityClass)
    {
        return GetCost(movementMode, passabilityClass) != IMPASSABLE;
    }

    /// <summary>
    /// Check if indices are valid.
    /// </summary>
    private bool IsValidIndex(int movementMode, int passabilityClass)
    {
        return movementMode >= 0 && movementMode < numMovementModes &&
               passabilityClass >= 0 && passabilityClass < numPassabilityClasses;
    }

    /// <summary>
    /// Create a default passability table with standard terrain types.
    /// This is based on typical Ultima/Nazghul terrain setup.
    /// </summary>
    public static PassabilityTable CreateDefault()
    {
        const int NUM_MODES = 4;
        const int NUM_CLASSES = 15;
        
        var ptable = new PassabilityTable(NUM_MODES, NUM_CLASSES);
        
        // Define passability classes (these match Terrain.Common definitions).
        const int PC_GRASS = 1;
        const int PC_ROAD = 2;
        const int PC_FOREST = 3;
        const int PC_HILLS = 4;
        const int PC_SHALLOW = 5;
        const int PC_WATER = 6;
        const int PC_DEEP_WATER = 7;
        const int PC_MOUNTAIN = 8;
        const int PC_WALL = 9;
        const int PC_LAVA = 10;
        const int PC_SWAMP = 11;
        const int PC_FIRE = 12;
        const int PC_ICE = 13;
        
        // Walking mode (0) - Normal on-foot travel
        ptable.SetCost((int)MovementMode.ModeIndex.Walking, PC_GRASS, 1);
        ptable.SetCost((int)MovementMode.ModeIndex.Walking, PC_ROAD, 1);
        ptable.SetCost((int)MovementMode.ModeIndex.Walking, PC_FOREST, 2);      // Slower
        ptable.SetCost((int)MovementMode.ModeIndex.Walking, PC_HILLS, 2);       // Slower
        ptable.SetCost((int)MovementMode.ModeIndex.Walking, PC_SHALLOW, 3);     // Can wade
        ptable.SetCost((int)MovementMode.ModeIndex.Walking, PC_WATER, IMPASSABLE);
        ptable.SetCost((int)MovementMode.ModeIndex.Walking, PC_DEEP_WATER, IMPASSABLE);
        ptable.SetCost((int)MovementMode.ModeIndex.Walking, PC_MOUNTAIN, IMPASSABLE);
        ptable.SetCost((int)MovementMode.ModeIndex.Walking, PC_WALL, IMPASSABLE);
        ptable.SetCost((int)MovementMode.ModeIndex.Walking, PC_LAVA, IMPASSABLE);
        ptable.SetCost((int)MovementMode.ModeIndex.Walking, PC_SWAMP, 3);       // Very slow
        ptable.SetCost((int)MovementMode.ModeIndex.Walking, PC_FIRE, 2);        // Can run through
        ptable.SetCost((int)MovementMode.ModeIndex.Walking, PC_ICE, 1);         // Slippery
        
        // Swimming mode (1) - In water without boat
        ptable.SetCost((int)MovementMode.ModeIndex.Swimming, PC_GRASS, 3);       // Slow on land
        ptable.SetCost((int)MovementMode.ModeIndex.Swimming, PC_ROAD, 3);
        ptable.SetCost((int)MovementMode.ModeIndex.Swimming, PC_FOREST, 4);
        ptable.SetCost((int)MovementMode.ModeIndex.Swimming, PC_HILLS, 4);
        ptable.SetCost((int)MovementMode.ModeIndex.Swimming, PC_SHALLOW, 1);     // Normal in water
        ptable.SetCost((int)MovementMode.ModeIndex.Swimming, PC_WATER, 1);
        ptable.SetCost((int)MovementMode.ModeIndex.Swimming, PC_DEEP_WATER, 2); // Slower in deep
        ptable.SetCost((int)MovementMode.ModeIndex.Swimming, PC_MOUNTAIN, IMPASSABLE);
        ptable.SetCost((int)MovementMode.ModeIndex.Swimming, PC_WALL, IMPASSABLE);
        ptable.SetCost((int)MovementMode.ModeIndex.Swimming, PC_LAVA, IMPASSABLE);
        ptable.SetCost((int)MovementMode.ModeIndex.Swimming, PC_SWAMP, 1);
        ptable.SetCost((int)MovementMode.ModeIndex.Swimming, PC_FIRE, IMPASSABLE);
        ptable.SetCost((int)MovementMode.ModeIndex.Swimming, PC_ICE, 1);
        
        // Flying mode (2) - Can go anywhere
        for (int i = 0; i < NUM_CLASSES; i++)
        {
            ptable.SetCost((int)MovementMode.ModeIndex.Flying, i, 1);
        }
        // Even flying can't go through solid walls
        ptable.SetCost((int)MovementMode.ModeIndex.Flying, PC_WALL, IMPASSABLE);
        
        // Sailing mode (3) - In boat
        ptable.SetCost((int)MovementMode.ModeIndex.Sailing, PC_GRASS, IMPASSABLE);
        ptable.SetCost((int)MovementMode.ModeIndex.Sailing, PC_ROAD, IMPASSABLE);
        ptable.SetCost((int)MovementMode.ModeIndex.Sailing, PC_FOREST, IMPASSABLE);
        ptable.SetCost((int)MovementMode.ModeIndex.Sailing, PC_HILLS, IMPASSABLE);
        ptable.SetCost((int)MovementMode.ModeIndex.Sailing, PC_SHALLOW, 2);     // Slow in shallow
        ptable.SetCost((int)MovementMode.ModeIndex.Sailing, PC_WATER, 1);       // Normal on water
        ptable.SetCost((int)MovementMode.ModeIndex.Sailing, PC_DEEP_WATER, 1);
        ptable.SetCost((int)MovementMode.ModeIndex.Sailing, PC_MOUNTAIN, IMPASSABLE);
        ptable.SetCost((int)MovementMode.ModeIndex.Sailing, PC_WALL, IMPASSABLE);
        ptable.SetCost((int)MovementMode.ModeIndex.Sailing, PC_LAVA, IMPASSABLE);
        ptable.SetCost((int)MovementMode.ModeIndex.Sailing, PC_SWAMP, 2);
        ptable.SetCost((int)MovementMode.ModeIndex.Sailing, PC_FIRE, IMPASSABLE);
        ptable.SetCost((int)MovementMode.ModeIndex.Sailing, PC_ICE, IMPASSABLE); // Can't sail on ice
        
        return ptable;
    }

    /// <summary>
    /// Get a human-readable string representation.
    /// </summary>
    public override string ToString()
    {
        return $"PassabilityTable({numMovementModes} modes × {numPassabilityClasses} classes)";
    }
}