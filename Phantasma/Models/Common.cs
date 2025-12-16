namespace Phantasma.Models;

public class Common
{
    // ===================================================================
    // INSTANCE STATE (Game Clock/Timing)
    // ===================================================================
    
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
    }
    
    // ===================================================================
    // CONFIGURATION FLAGS
    // ===================================================================

    /*
     * Experimental code that let's NPC parties move around the wilderness while
     * the player is in a temporary combat map.
     */
    public const int CONFIG_CONCURRENT_WILDERNESS = 1;
    
    /// <summary>
    /// Whether movement costs action points for characters.
    /// </summary>
    public const bool CONFIG_MOVEMENT_COST_FOR_CHARACTERS = false;
    
    // ===================================================================
    // DIRECTIONS
    // ===================================================================

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
    
    // ===================================================================
    // TIMING
    // ===================================================================
    
    // Timing
    public const int ANIMATION_TICKS = 10;	// Ticks between animation frame changes
    public const int MS_PER_TICK = 100;
    
    // ===================================================================
    // ACTION POINTS
    // ===================================================================
    
    /// <summary>
    /// Base action points cost for standard actions.
    /// Matches Nazghul's NAZGHUL_BASE_ACTION_POINTS.
    /// </summary>
    public const int NAZGHUL_BASE_ACTION_POINTS = 1;
    
    /// <summary>Maximum action points per turn (default).</summary>
    public const int DEFAULT_MAX_ACTION_POINTS = 10;
    
    // ===================================================================
    // TYPE IDs (TIDs) - Object Instances
    // Matches Nazghul common.h exactly.
    // ===================================================================
    
    public const int OBJECT_ID = 1;
    public const int VEHICLE_ID = OBJECT_ID + 1;        // 2
    public const int CHARACTER_ID = OBJECT_ID + 2;      // 3
    public const int MOONGATE_ID = OBJECT_ID + 3;       // 4
    public const int PORTAL_ID = OBJECT_ID + 4;         // 5
    public const int PARTY_ID = OBJECT_ID + 5;          // 6
    public const int SPRITE_ID = OBJECT_ID + 7;         // 8
    
    // ===================================================================
    // TYPE IDs (TIDs) - ObjectType Definitions
    // ===================================================================
    
    public const int OBJECT_TYPE_ID = 100;
    public const int ARMS_TYPE_ID = 101;
    public const int ITEM_TYPE_ID = 102;
    public const int VEHICLE_TYPE_ID = 103;
    public const int ORDNANCE_TYPE_ID = 104;
    public const int CHARACTER_TYPE_ID = 105;
    public const int ENTITY_TYPE_ID = 106;
    public const int MOONGATE_TYPE_ID = 107;
    public const int REAGENT_TYPE_ID = 108;
    public const int SPELL_TYPE_ID = 109;
    // AMMO_TYPE_ID = 110 -- obsolete in Nazghul
    public const int FIELD_TYPE_ID = 111;
    public const int FOOD_TYPE_ID = 112;
    public const int TRAP_TYPE_ID = 113;
    public const int PARTY_TYPE_ID = 114;
    
    // ===================================================================
    // TYPE IDs (TIDs) - Other Definitions
    // ===================================================================
    
    public const int RESPONSE_TYPE_ID = 116;
    public const int CONVERSATION_TYPE_ID = 117;
    public const int OCC_ID = 118;               // Occupation
    public const int SPECIES_ID = 119;
    public const int SCHEDULE_ID = 120;
    public const int PLACE_ID = 121;
    public const int MAP_ID = 122;
    public const int TERRAIN_PALETTE_ID = 124;
    public const int TERRAIN_ID = 125;
    public const int MECH_TYPE_ID = 126;
    public const int MECH_ID = 127;
    public const int IMAGES_ID = 128;
    public const int FORMATION_TYPE_ID = 129;
    
    // ===================================================================
    // DAMAGES
    // ===================================================================
    
    public const int DAMAGE_FIRE = 10;
    public const int DAMAGE_POISON = 1;
    public const int DAMAGE_STARVATION = 1;
    public const int DAMAGE_ACID = 10;
    public const int DAMAGE_BOMB = 25;
    
    // ===================================================================
    // TARGETS
    // ===================================================================
    
    public const int TARG_NONE = 0;
    public const int TARG_SELF = 1;
    public const int TARG_FRIEND = 2;
    
    // ===================================================================
    // SPELL TARGETS
    // ===================================================================
    
    public const int SPELL_TARGET_NONE = 0;
    public const int SPELL_TARGET_CHARACTER = 1;
    public const int SPELL_TARGET_MECH = 2;
    public const int SPELL_TARGET_DIRECTION = 3;
    public const int SPELL_TARGET_LOCATION = 4;
    public const int SPELL_TARGET_UP = 5;
    public const int SPELL_TARGET_DOWN = 6;
    public const int SPELL_TARGET_ALL_PARTY_MEMBERS = 7;
    public const int SPELL_TARGET_CASTER_LOCATION = 8;
    public const int SPELL_TARGET_PARTY_MEMBER = 9;
    
    // ===================================================================
    // SERVICES
    // ===================================================================
    
    public const int SRV_HEAL = 1;
    public const int SRV_CURE = 2;
    public const int SRV_RESURRECT = 3;
    public const int SRV_MIN = SRV_HEAL;
    public const int SRV_MAX = SRV_RESURRECT;
    
    // ===================================================================
    // CONTEXTS
    // ===================================================================
    
    public const int CONTEXT_WILDERNESS = 1;
    public const int CONTEXT_TOWN = 2;
    
    // ===================================================================
    // RESTING
    // ===================================================================
    
    public const int HP_RECOVERED_PER_HOUR_OF_REST = 3;
    public const int MANA_RECOVERED_PER_HOUR_OF_REST = 10;
    public const int PROB_AWAKEN = 25;
    
    // ===================================================================
    // MISC CONSTANTS
    // ===================================================================
    
    public const int MAX_N_REAGENTS = 32;
    public const int WIND_CHANGE_PROBABILITY = 5;
    public const int PLAYER_MAX_PROGRESS = 100;
    public const int MAX_NAME_LEN = 32;
    public const int MIN_VISION_RADIUS = 1;
    public const int HP_PER_LVL = 30;
    public const int MAX_ATTRIBUTE_VALUE = 999;
    public const int MAX_SPEED = 100;
    public const int WILDERNESS_SCALE = 32;
    public const int NON_WILDERNESS_SCALE = 1;
    public const int TURNS_PER_FOOD = 8;  // Simplified from TURNS_PER_DAY/3
    
    // ===================================================================
    // LIGHTING
    // ===================================================================
    
    public const int MAX_AMBIENT_LIGHT = 255;
    public const int MAX_SUNLIGHT = 255;
    public const int MAX_MOONLIGHT = 128;
    public const int MIN_PLAYER_LIGHT = 128;
    
    // ===================================================================
    // VEHICLE
    // ===================================================================
    
    public const int TURNS_TO_FIRE_VEHICLE_WEAPON = 2;
    
    // ===================================================================
    // HELPER METHODS
    // ===================================================================
    
    /// <summary>
    /// Check if a direction value is valid.
    /// </summary>
    public static bool IsLegalDirection(int dir)
    {
        return dir >= 0 && dir < NUM_DIRECTIONS;
    }
    
    /// <summary>
    /// Check if a direction is a planar direction (not UP/DOWN).
    /// </summary>
    public static bool IsPlanarDirection(int dir)
    {
        return dir >= 0 && dir < NUM_PLANAR_DIRECTIONS;
    }
    
    /// <summary>
    /// Get the X delta for a direction.
    /// </summary>
    public static int DirectionToDx(int dir)
    {
        return dir switch
        {
            NORTHWEST or WEST or SOUTHWEST => -1,
            NORTHEAST or EAST or SOUTHEAST => 1,
            _ => 0
        };
    }
    
    /// <summary>
    /// Get the Y delta for a direction.
    /// </summary>
    public static int DirectionToDy(int dir)
    {
        return dir switch
        {
            NORTHWEST or NORTH or NORTHEAST => -1,
            SOUTHWEST or SOUTH or SOUTHEAST => 1,
            _ => 0
        };
    }
    
    /// <summary>
    /// Convert dx, dy to a direction.
    /// </summary>
    public static int DeltaToDirection(int dx, int dy)
    {
        // Clamp to -1, 0, 1
        dx = dx < 0 ? -1 : (dx > 0 ? 1 : 0);
        dy = dy < 0 ? -1 : (dy > 0 ? 1 : 0);
        
        return (dx, dy) switch
        {
            (-1, -1) => NORTHWEST,
            (0, -1) => NORTH,
            (1, -1) => NORTHEAST,
            (-1, 0) => WEST,
            (0, 0) => HERE,
            (1, 0) => EAST,
            (-1, 1) => SOUTHWEST,
            (0, 1) => SOUTH,
            (1, 1) => SOUTHEAST,
            _ => DIRECTION_NONE
        };
    }
    
    /// <summary>
    /// Get the opposite direction.
    /// </summary>
    public static int OppositeDirection(int dir)
    {
        return dir switch
        {
            NORTHWEST => SOUTHEAST,
            NORTH => SOUTH,
            NORTHEAST => SOUTHWEST,
            WEST => EAST,
            EAST => WEST,
            SOUTHWEST => NORTHEAST,
            SOUTH => NORTH,
            SOUTHEAST => NORTHWEST,
            UP => DOWN,
            DOWN => UP,
            _ => DIRECTION_NONE
        };
    }
    
    /// <summary>
    /// Get direction name as string.
    /// </summary>
    public static string DirectionToString(int dir)
    {
        return dir switch
        {
            NORTHWEST => "northwest",
            NORTH => "north",
            NORTHEAST => "northeast",
            WEST => "west",
            HERE => "here",
            EAST => "east",
            SOUTHWEST => "southwest",
            SOUTH => "south",
            SOUTHEAST => "southeast",
            UP => "up",
            DOWN => "down",
            _ => "none"
        };
    }
}
