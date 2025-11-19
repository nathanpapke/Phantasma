namespace Phantasma.Models;

/// <summary>
/// UI Cursor for Selection
/// </summary>
public class Cursor : Object
{
    public override ObjectLayer Layer => ObjectLayer.Cursor;
    
    // Cursor is usually a Singleton/special case
}
