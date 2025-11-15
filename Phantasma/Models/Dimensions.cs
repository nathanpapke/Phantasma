using System.Collections.Generic;

namespace Phantasma.Models;

public class Dimensions
{
    private Dictionary<string, string> Cfg;
    private int mapSize;
    
    public const int STAT_CHARS_PER_LINE = 46;

    public const int TILE_W = 32;
    public const int TILE_H = 32;
    public const int ASCII_W = 8;
    public const int ASCII_H = 16;
    public const int BORDER_W = 16;
    public const int BORDER_H = 16;

    public const int MIN_MAP_SIZE = 11;
    public const int MAX_MAP_SIZE = 19;
    public const int DEF_MAP_SIZE = 19;
    
    public int STATUS_MAX_MSG_SZ;
    public int SCREEN_W;
    public int SCREEN_H;
    public int CONSOLE_MAX_MSG_SZ;

    public int MAP_TILE_W;
    public int MAP_TILE_H;
    public int MAP_X;
    public int MAP_Y;
    public int MAP_W;
    public int MAP_H;

    public int CMD_X;
    public int CMD_Y;
    public int CMD_W;
    public int CMD_H;

    public int STAT_X;
    public int STAT_Y;
    public int STAT_W;
    public int STAT_H;
    public int STAT_H_MAX;

    public int FOOGOD_X;
    public int FOOGOD_Y;
    public int FOOGOD_W;
    public int FOOGOD_H;

    public int WIND_X;
    public int WIND_Y;
    public int WIND_W;
    public int WIND_H;

    public int CONS_X;
    public int CONS_Y;
    public int CONS_W;
    public int CONS_H;
    public int CONS_LINES;

    public int SKY_X;
    public int SKY_Y;
    public int SKY_W;
    public int SKY_H;
    public int SKY_SPRITE_W;
    
    /* dimensions_get_map_size -- figure out the biggest map window that will
     * satisfy the screen dimensions.  */
    public static int GetMapSize(string dimensions)
    {
        if (dimensions.Length == 0)
        {
            //warning
            return -1;
        }
        
        // Look up map size for screen dimension.

        return -1;
    }

    public Dimensions()
    {
        Cfg = new Dictionary<string, string>();

        mapSize = 20; //GetMapSize(Cfg.GetValueOrDefault("screen-dims")); // TODO: update!!!
        MAP_TILE_W = mapSize;
        MAP_TILE_H = mapSize;

        MAP_X = BORDER_W;
        MAP_Y = BORDER_H;
        MAP_W = (TILE_W * MAP_TILE_W);
        MAP_H = (TILE_H * MAP_TILE_H);

        CMD_X = MAP_X;
        CMD_Y = (MAP_Y + MAP_H + BORDER_H);
        CMD_W = MAP_W;
        CMD_H = ASCII_H;

        SCREEN_H = (BORDER_H * 3 + MAP_H + CMD_H);

        STATUS_MAX_MSG_SZ = 128;
        STAT_X = (MAP_X + MAP_W + BORDER_W);
        STAT_Y = BORDER_H;
        STAT_W = ( /*BORDER_W * 2*/ +ASCII_W * STAT_CHARS_PER_LINE);
        STAT_H = (3 * TILE_H);
        STAT_H_MAX = (16 * TILE_H);

        CONS_X = STAT_X;
        CONS_Y = (FOOGOD_Y + FOOGOD_H + BORDER_H);
        CONS_W = STAT_W;
        CONS_H = (SCREEN_H - BORDER_H - CONS_Y);
        CONS_LINES = (CONS_H / ASCII_H);

        CONSOLE_MAX_MSG_SZ = (CONS_W / ASCII_W);

        FOOGOD_X = STAT_X;
        FOOGOD_Y = (STAT_Y + STAT_H + BORDER_H);
        FOOGOD_W = STAT_W;
        FOOGOD_H = (2 * ASCII_H);

        WIND_W = ("wind:northeast".Length * ASCII_W);
        WIND_H = BORDER_H;
        WIND_X = (BORDER_W + (MAP_W - WIND_W) / 2);
        WIND_Y = (MAP_Y + MAP_H);

        //SKY_W =   MOON_WINDOW_W;
        SKY_H = BORDER_H;
        SKY_X = (MAP_X + (MAP_W - SKY_W) / 2);
        SKY_Y = 0;
        SKY_SPRITE_W = (TILE_W / 2);

        SCREEN_W = (BORDER_W * 3 + MAP_W + CONS_W);
    }
}