using System;
using System.Collections.Generic;
using System.IO;
using IronScheme;
using IronScheme.Runtime;
using IronScheme.Scripting;

namespace Phantasma.Models;

public partial class Kernel
{
    private static readonly HashSet<string> loadedFiles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Clear the loaded files tracking. Call when starting a new game.
    /// </summary>
    public static void ClearLoadedFiles() => loadedFiles.Clear();

    /// <summary>
    /// (kern-include filename)
    /// Loads another Scheme file.
    /// Resolves relative paths against the scripts directory.
    /// Skips files that have already been loaded.
    /// </summary>
    public static object Include_Old(object args)
    {
        try
        {
            string rawPath = ExtractFilename(args);
            
            if (string.IsNullOrEmpty(rawPath))
            {
                Console.Error.WriteLine("[kern-include] Error: could not extract filename from args.");
                return "nil".Eval();
            }
            
            // Resolve the path.
            string path = Phantasma.ResolvePath(rawPath);
            
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"[kern-include] File not found: {path}");
                return "nil".Eval();
            }
            
            // Check if already loaded (prevents double-loading).
            string normalizedPath = Path.GetFullPath(path);
            if (loadedFiles.Contains(normalizedPath))
            {
                // Already loaded - skip silently (this is normal)
                return "nil".Eval();
            }
            
            // Mark as loaded BEFORE loading to handle circular includes.
            loadedFiles.Add(normalizedPath);
            
            // Get the kernel and load the file.
            var kernel = Phantasma.Kernel;
            if (kernel == null)
            {
                Console.Error.WriteLine("[kern-include] Error: Phantasma.Kernel is null");
                return "nil".Eval();
            }
            
            // Load the file - LoadSchemeFile handles its own errors.
            kernel.LoadSchemeFile(path);
            
            return "nil".Eval();
        }
        catch (Exception ex)
        {
            // Catch ANY exception and log it - never let it propagate.
            Console.Error.WriteLine($"[kern-include] Unexpected error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.Error.WriteLine($"[kern-include] Inner: {ex.InnerException.Message}");
            }
            return "nil".Eval();
        }
    }

    /// <summary>
    /// Extract the filename from various argument formats IronScheme might pass.
    /// </summary>
    private static string ExtractFilename(object args)
    {
        if (args == null)
        {
            return null;
        }
        
        string rawPath = null;
        
        // Handle Cons list: (kern-include "file.scm") -> Cons with car = "file.scm"
        if (args is Cons cons)
        {
            var firstArg = cons.car;
            if (firstArg is string s)
            {
                rawPath = s;
            }
            else if (firstArg != null)
            {
                rawPath = firstArg.ToString();
            }
        }
        // Handle direct string.
        else if (args is string str)
        {
            rawPath = str;
        }
        // Handle symbol.
        else if (args is SymbolId sym)
        {
            rawPath = SymbolTable.IdToString(sym);
        }
        // Fallback: convert to string
        else
        {
            rawPath = args.ToString();
        }
        
        if (rawPath == null)
        {
            return null;
        }
        
        // Handle quoted strings - strip quotes if present.
        if (rawPath.StartsWith("\"") && rawPath.EndsWith("\"") && rawPath.Length > 2)
        {
            rawPath = rawPath.Substring(1, rawPath.Length - 2);
        }
        
        return rawPath;
    }

    /// <summary>
    /// Resolve a relative path to an absolute path using the scripts directory.
    /// </summary>
    private static string ResolvePath(string rawPath)
    {
        if (Path.IsPathRooted(rawPath))
        {
            return rawPath;
        }
        
        string scriptsDir;
        if (Phantasma.Configuration.TryGetValue("include-dirname", out string dir))
        {
            scriptsDir = dir;
        }
        else
        {
            scriptsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
        }
        
        return Path.Combine(scriptsDir, rawPath);
    }
}
