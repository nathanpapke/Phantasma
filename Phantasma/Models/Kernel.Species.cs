namespace Phantasma.Models;

public partial class Kernel
{
    /// <summary>
    /// (kern-species-get-hp-mod species)
    /// </summary>
    /// <param name="species"></param>
    /// <returns></returns>
    public static object SpeciesGetHpMod(object[] args)
    {
        var species = args.Length > 0 ? args[0] : null;
        
        if (species == null || IsNil(species))
            return 0;
    
        Species? sp = species as Species?;
    
        if (sp == null && species is string tag)
        {
            string cleanTag = tag.TrimStart('\'').Trim('"');
            var resolved = Phantasma.GetRegisteredObject(cleanTag);
            if (resolved is Species s)
                sp = s;
        }
    
        return sp?.HpMod ?? 0;
    }
    
    /// <summary>
    /// (kern-species-get-hp-mult species) -> int
    /// Gets the HP multiplier per level for a species.
    /// </summary>
    public static object SpeciesGetHpMult(object[] args)
    {
        var species = args.Length > 0 ? args[0] : null;
        
        if (species == null || IsNil(species))
            return 0;
        
        Species? sp = species as Species?;
        if (sp == null && species is string tag)
        {
            string cleanTag = tag.TrimStart('\'').Trim('"');
            var resolved = Phantasma.GetRegisteredObject(cleanTag);
            if (resolved is Species s)
                sp = s;
        }
        
        return sp?.HpMult ?? 0;
    }
    
    /// <summary>
    /// (kern-species-get-mp-mod species)
    /// </summary>
    /// <param name="species"></param>
    /// <returns></returns>
    public static object SpeciesGetMpMod(object[] args)
    {
        var species = args.Length > 0 ? args[0] : null;
        
        if (species == null || IsNil(species))
            return 0;
        
        Species? sp = species as Species?;
        if (sp == null && species is string tag)
        {
            string cleanTag = tag.TrimStart('\'').Trim('"');
            var resolved = Phantasma.GetRegisteredObject(cleanTag);
            if (resolved is Species s)
                sp = s;
        }
        
        return sp?.MpMod ?? 0;
    }
    
    /// <summary>
    /// (kern-species-get-mp-mult species)
    /// </summary>
    public static object SpeciesGetMpMult(object[] args)
    {
        var species = args.Length > 0 ? args[0] : null;
        
        if (species == null || IsNil(species))
            return 0;
        
        Species? sp = species as Species?;
        if (sp == null && species is string tag)
        {
            string cleanTag = tag.TrimStart('\'').Trim('"');
            var resolved = Phantasma.GetRegisteredObject(cleanTag);
            if (resolved is Species s)
                sp = s;
        }
        
        return sp?.MpMult ?? 0;
    }
}
