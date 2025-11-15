namespace Phantasma.Models;

public class Common
{
    public int Turn;
    public int Tick;
    public int AnimationTicks;
    public int TickMilliseconds;
    //public int WindDirection;
    public int ShowAllTerrain = 0;

    public Common()
    {
        Turn = 0;
        Tick = 0;
        //srand(0);
    }

    /* Constants *****************************************************************/

    /*
     * Experimental code that let's NPC parties move around the wilderness while
     * the player is in a temporary combat map.
     */
    public const int CONFIG_CONCURRENT_WILDERNESS = 1;
    
    // Directions
    public const int DIRECTION_NONE = -1;
    public const int NORTHWEST = 0;
    public const int NORTH = 1;
    public const int NORTHEAST = 2;
    public const int WEST = 3;
    public const int HERE = 4;
    public const int EAST = 5;
    public const int SOUTHWEST = 6;
    public const int SOUTH = 7;
    public const int SOUTHEAST = 8;
    public const int UP = 9;
    public const int DOWN = 10;
    public const int NUM_DIRECTIONS = 11;
    public const int NUM_PLANAR_DIRECTIONS = 9;
    public const int NUM_WIND_DIRECTIONS = 9;
    //public const int IS_LEGAL_DIRECTION(dir) = ((dir)>=0 && (dir)<NUM_DIRECTIONS);
    
    // Timing
    public const int ANIMATION_TICKS = 10;	// Ticks between animation frame changes
    public const int MS_PER_TICK = 100;
}