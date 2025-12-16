using System;
using IronScheme;
using IronScheme.Runtime;

namespace Phantasma.Models;

public partial class Kernel
{
    /// <summary>
    /// (kern-arms-type-get-range arms-type)
    /// Returns the range value of an arms type.
    /// </summary>
    public static object ArmsTypeGetRange(object armsType)
    {
        if (armsType is not ArmsType arms)
        {
            Console.WriteLine("[ERROR] kern-arms-type-get-range: not an arms type");
            return 0;
        }
        
        return arms.Range;
    }
    
    /// <summary>
    /// (kern-arms-type-get-ammo-type arms-type)
    /// Returns the ammo type for a missile weapon, or nil if none.
    /// </summary>
    public static object ArmsTypeGetAmmoType(object armsType)
    {
        if (armsType is not ArmsType arms)
        {
            Console.WriteLine("[ERROR] kern-arms-type-get-ammo-type: not an arms type");
            return Builtins.Unspecified;
        }
        
        var ammoType = arms.GetAmmoType();
        return ammoType ?? (object)Builtins.Unspecified;
    }
}
