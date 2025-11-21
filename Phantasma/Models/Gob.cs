using IronScheme;
using IronScheme.Hosting;

namespace Phantasma.Models;

// TODO: Possibly rename to GameObject.  Explore for AI utilization
public struct Gob
{
    // Scheme
    public string Scheme;
    
    // Pointer
    public int Flags;
    public int RefCount;
}