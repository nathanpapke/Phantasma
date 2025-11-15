using System;
using System.Collections.Generic;

using Phantasma.Models;

namespace Phantasma.Binders;

public class Phantasma
{
    //
    private Common common;
    
    private static string LoadFileName = "";

    private int FullScreenMode = 0;
    private int DeveloperMode = 0;
    private int ExitProgram = 0;
    private int PrintOptions = 0;

    private static string ProgramName = "Phantasma";
    private static string PackageVersion = "1.0";

    private Dictionary<string, string> Cfg;
    

    private static void PrintVersion()
    {
        Console.Write("{0} {1}\r\n", ProgramName, PackageVersion);
        Console.Write("Ported from Nazghul, Copyright (C) 2003 Gordon McNutt & Sam Glasby\r\n"+
                      "to .NET in 2025 by Nathan Papke.\r\n"+
                      "{0} comes with NO WARRANTY,\r\n"+
                      "to the extent permitted by law.\r\n"+
                      "You may redistribute copies of {0}\r\n"+
                      "under the terms of the GNU General Public License.\r\n"+
                      "For more information about these matters,\r\n"+
                      "see the file named COPYING.\r\n",
            ProgramName);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:  {0} [options] <load-file>", ProgramName);
        Console.WriteLine("Options:");
        Console.WriteLine("    -b: bot"); //added
        Console.WriteLine("    -h: help);");
        Console.WriteLine("    -l: line of sight <floodfill|angband>"); //added
        Console.WriteLine("    -v: version");
        Console.WriteLine("    -d: developer mode");
        Console.WriteLine("    -f: fullscreen mode);");
        Console.WriteLine("    -t: tick <period in msec>");
        Console.WriteLine("    -a: animation <period in ticks>");
        Console.WriteLine("    -s: sound <0 to disable>");
        Console.WriteLine("    -R: recorder <filename>");
        Console.WriteLine("    -P: playback <filename>");
        Console.WriteLine("    -S: speed <playback ms delay>");
        Console.WriteLine("    -I: game data dir");
        Console.WriteLine("    -G: save game dir");
        Console.WriteLine("    -r: screen size <pixels> (eg, 640x480))):" );
        Console.WriteLine("    -T: show all terrain");
        Console.WriteLine("<load-file> is the session to load.");
    }

    private void ParseArgs(string[] args)
    {
        int c = 0;
        
        common.TickMilliseconds = Common.MS_PER_TICK;
        common.AnimationTicks = Common.ANIMATION_TICKS;

        while (c < args.Length && args[c].Substring(0, 1) == "-")
        {
            switch (args[c].Substring(1, 1))
            {
                case "t":
                    common.TickMilliseconds = int.Parse(args[c].Substring(3));
                    break;
                case "a":
                    common.AnimationTicks = int.Parse(args[c].Substring(3));
                    break;
                case "s":
                    Cfg["sound-enabled"] = int.Parse(args[c].Substring(3)) != 0 ? "yes" : "no";
                    break;
                case "T":
                    common.ShowAllTerrain = 1;
                    break;
                case "d":
                    DeveloperMode = 1;
                    //DEBUG = 1;
                    //VERBOSE = 1;
                break;
                case "f":
                    FullScreenMode = 1;
                    break;
                case "R":
                    /* Set the filename for recording keystrokes. */
                    Cfg["record-filename"] = args[c].Substring(3);
                    break;
                case "S":
                    /* Set the speed to play back recorded keystrokes. */
                    Cfg["playback-speed"] = args[c].Substring(3);
                    break;
                case "P":
                    /* Set the file to play back keystrokes from. */
                    Cfg["playback-filename"] = args[c].Substring(3);
                    break;
                case "I":
                    /* Set the directory for read-only game and cfg
                     * files. */
                    Cfg["include-dirname"] = args[c].Substring(3);
                    break;
                case "G":
                    /* Set the directory for read-write game and cfg
                     * files. */
                    Cfg["saved-games-dirname"] = args[c].Substring(3);
                    break;
                case "v":
                    PrintVersion();
                    Environment.Exit(0);
                    break;
                case "h":
                    PrintUsage();
                    Environment.Exit(0);
                    break;
                case "o":
                    PrintOptions = 1;
                    break;
                case "r":
                    /* Set the screen dimensions. */
                    Cfg.Add("screen-dims", args[c].Substring(3));
                    break;
                default:
                    PrintUsage();
                    Environment.Exit(-1);
                    break;
            }

            c++;
        }
        
        // --------------------------------------------------------------------
        // Any remaining option is assumed to be the save-file to load the game
        // from. If there is none then abort.
        // --------------------------------------------------------------------
        if (c < args.Length)
            LoadFileName = args[c];
    }

    /// <summary>
    /// Check if developer mode is enabled.
    /// </summary>
    public bool IsDeveloperMode()
    {
        return DeveloperMode == 1;
    }

    /// <summary>
    /// Check if fullscreen mode is requested.
    /// </summary>
    public bool IsFullScreenMode()
    {
        return FullScreenMode == 1;
    }
}