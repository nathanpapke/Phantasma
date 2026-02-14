using System;

namespace Phantasma.Models;

public partial class Kernel
{
    /// <summary>
    /// (kern-dtable-get faction1 faction2)
    /// </summary>
    public static object DiplomacyTableGet(object[] args)
    {
        var f1 = args.Length > 0 ? args[0] : null;
        var f2 = args.Length > 1 ? args[1] : null;
        
        var dtable = Phantasma.MainSession.DiplomacyTable;
        return dtable?.Get(Convert.ToInt32(f1), Convert.ToInt32(f2)) ?? 0;
    }

    /// <summary>
    /// (kern-dtable-set faction1 faction2 value)
    /// </summary>
    public static object DiplomacyTableSet(object[] args)
    {
        var f1 = args.Length > 0 ? args[0] : null;
        var f2 = args.Length > 1 ? args[1] : null;
        var val = args.Length > 2 ? args[2] : null;
        
        var dtable = Phantasma.MainSession.DiplomacyTable;
        if (dtable == null) return false;
        int v = Convert.ToInt32(val);
        dtable.Set(Convert.ToInt32(f1), Convert.ToInt32(f2), v);
        return v;
    }

    /// <summary>
    /// (kern-dtable-inc faction1 faction2)
    /// </summary>
    public static object DiplomacyTableIncrement(object[] args)
    {
        var f1 = args.Length > 0 ? args[0] : null;
        var f2 = args.Length > 1 ? args[1] : null;
        
        var dtable = Phantasma.MainSession.DiplomacyTable;
        if (dtable == null) return false;
        dtable.Increment(Convert.ToInt32(f1), Convert.ToInt32(f2));
        return dtable.Get(Convert.ToInt32(f1), Convert.ToInt32(f2));
    }

    /// <summary>
    /// (kern-dtable-dec faction1 faction2)
    /// </summary>
    public static object DiplomacyTableDecrement(object[] args)
    {
        var f1 = args.Length > 0 ? args[0] : null;
        var f2 = args.Length > 1 ? args[1] : null;
        
        var dtable = Phantasma.MainSession.DiplomacyTable;
        if (dtable == null) return false;
        dtable.Decrement(Convert.ToInt32(f1), Convert.ToInt32(f2));
        return dtable.Get(Convert.ToInt32(f1), Convert.ToInt32(f2));
    }
}
