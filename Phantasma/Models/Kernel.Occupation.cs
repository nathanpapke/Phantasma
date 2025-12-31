namespace Phantasma.Models;

public partial class Kernel
{
    /// <summary>
    /// (kern-occ-get-hp-mod occ)
    /// </summary>
    /// <param name="occ"></param>
    /// <returns></returns>
    public static object OccupationGetHpMod(object occ)
    {
        Occupation? occupation = occ as Occupation?;
    
        if (occupation == null && occ is string tag)
        {
            string cleanTag = tag.TrimStart('\'').Trim('"');
            var resolved = Phantasma.GetRegisteredObject(cleanTag);
            if (resolved is Occupation o)
                occupation = o;
        }
    
        return occupation?.HpMod ?? 0;
    }
    
    /// <summary>
    /// (kern-occ-get-hp-mult occ)
    /// </summary>
    /// <param name="occ"></param>
    /// <returns></returns>
    public static object OccupationGetHpMult(object occ)
    {
        if (occ == null || IsNil(occ))
            return 0;
    
        Occupation? occupation = occ as Occupation?;
        if (occupation == null && occ is string tag)
        {
            string cleanTag = tag.TrimStart('\'').Trim('"');
            var resolved = Phantasma.GetRegisteredObject(cleanTag);
            if (resolved is Occupation o)
                occupation = o;
        }
    
        return occupation?.HpMult ?? 0;
    }
    
    /// <summary>
    /// (kern-occ-get-mp-mod occ)
    /// </summary>
    /// <param name="occ"></param>
    /// <returns></returns>
    public static object OccupationGetMpMod(object occ)
    {
        if (occ == null || IsNil(occ))
            return 0;
        
        Occupation? occupation = occ as Occupation?;
        if (occupation == null && occ is string tag)
        {
            string cleanTag = tag.TrimStart('\'').Trim('"');
            var resolved = Phantasma.GetRegisteredObject(cleanTag);
            if (resolved is Occupation o)
                occupation = o;
        }
        
        return occupation?.MpMod ?? 0;
    }
    
    /// <summary>
    /// (kern-occ-get-mp-mult occ)
    /// </summary>
    /// <param name="occ"></param>
    /// <returns></returns>
    public static object OccupationGetMpMult(object occ)
    {
        if (occ == null || IsNil(occ))
            return 0;
        
        Occupation? occupation = occ as Occupation?;
        if (occupation == null && occ is string tag)
        {
            string cleanTag = tag.TrimStart('\'').Trim('"');
            var resolved = Phantasma.GetRegisteredObject(cleanTag);
            if (resolved is Occupation o)
                occupation = o;
        }
        
        return occupation?.MpMult ?? 0;
    }
}
