using IronScheme;
using IronScheme.Hosting;

namespace Phantasma.Models;

public struct Gob
{
    //Scheme
    public string Scheme;
    
    //Pointer
    public int Flags;
    public int RefCount;
}