using System;
using IronScheme.Runtime;

namespace Phantasma.Models;

public partial class Kernel
{
    /// <summary>
    /// (kern-astral-body-get-phase astral_body)
    /// Returns the current phase index of an astral body.
    /// </summary>
    /// <param name="bodyObj"></param>
    /// <returns></returns>
    public static object AstralBodyGetPhase(object bodyObj)
    {
        if (bodyObj is not AstralBody body)
        {
            Console.WriteLine("[AstralBodyGetPhase] Error: not an astral body");
            return 0;
        }
        
        return body.PhaseIndex;
    }
    
    /// <summary>
    /// (kern-astral-body-get-gob astral_body)
    /// Returns the gob attached to an astral body.
    /// </summary>
    /// <param name="bodyObj"></param>
    /// <returns></returns>
    public static object AstralBodyGetGob(object bodyObj)
    {
        if (bodyObj is not AstralBody body)
        {
            Console.WriteLine("[AstralBodyGetGob] Error: not an astral body");
            return Builtins.Unspecified;
        }
        
        if (body.Gob == null)
        {
            Console.WriteLine($"[AstralBodyGetGob] Error: no gob for {body.Name}");
            return Builtins.Unspecified;
        }
        
        return body.Gob;
    }
    
    /// <summary>
    /// (kern-astral-body-set-gob astral_body gob)
    /// Attaches a Gob to an astral body.
    /// </summary>
    /// <param name="bodyObj"></param>
    /// <param name="gobObj"></param>
    /// <returns></returns>
    public static object AstralBodySetGob(object bodyObj, object gobObj)
    {
        if (bodyObj is not AstralBody body)
        {
            Console.WriteLine("[AstralBodySetGob] Error: not an astral body");
            return Builtins.Unspecified;
        }
        
        // Create Gob struct from the Scheme object.
        body.Gob = new Gob
        {
            SchemeData = gobObj?.ToString(),
            Flags = 0,
            RefCount = 1
        };
        
        return Builtins.Unspecified;
    }
}
