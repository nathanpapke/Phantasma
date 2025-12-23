using System;

namespace Phantasma.Models;

public partial class Kernel
{
    /// <summary>
    /// (kern-dtable-get faction1 faction2)
    /// </summary>
    public static object DiplomacyTableGet(object f1, object f2)
    {
        var dtable = Phantasma.MainSession.DiplomacyTable;
        return dtable?.Get(Convert.ToInt32(f1), Convert.ToInt32(f2)) ?? 0;
    }

    /// <summary>
    /// (kern-dtable-set faction1 faction2 value)
    /// </summary>
    public static object DiplomacyTableSet(object f1, object f2, object val)
    {
        var dtable = Phantasma.MainSession.DiplomacyTable;
        if (dtable == null) return false;
        int v = Convert.ToInt32(val);
        dtable.Set(Convert.ToInt32(f1), Convert.ToInt32(f2), v);
        return v;
    }

    /// <summary>
    /// (kern-dtable-inc faction1 faction2)
    /// </summary>
    public static object DiplomacyTableIncrement(object f1, object f2)
    {
        var dtable = Phantasma.MainSession.DiplomacyTable;
        if (dtable == null) return false;
        dtable.Increment(Convert.ToInt32(f1), Convert.ToInt32(f2));
        return dtable.Get(Convert.ToInt32(f1), Convert.ToInt32(f2));
    }

    /// <summary>
    /// (kern-dtable-dec faction1 faction2)
    /// </summary>
    public static object DiplomacyTableDecrement(object f1, object f2)
    {
        var dtable = Phantasma.MainSession.DiplomacyTable;
        if (dtable == null) return false;
        dtable.Decrement(Convert.ToInt32(f1), Convert.ToInt32(f2));
        return dtable.Get(Convert.ToInt32(f1), Convert.ToInt32(f2));
    }
}
