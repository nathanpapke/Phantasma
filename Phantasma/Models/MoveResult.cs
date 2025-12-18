namespace Phantasma.Models;

public enum MoveResult
{
    Ok,
    OffMap,
    EnterSubplace,
    Impassable,
    Occupied,
    EnterCombat,
    NullPlace,
    NoDestination
}
