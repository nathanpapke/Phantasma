using IronScheme;
using IronScheme.Hosting;

namespace Phantasma.Models;

/// <summary>
/// Game Object Block - Holds arbitrary Scheme data attached to game objects.
/// 
/// Gobs are the primary mechanism for storing quest state, NPC memory,
/// and other script-defined data. The Scheme data can be any valid
/// Scheme value: lists, symbols, numbers, etc.
/// </summary>
public struct Gob
{
    /// <summary>
    /// The Scheme data attached to this gob.
    /// This is the actual IronScheme object (list, pair, symbol, etc.).
    /// that scripts read and write via kern-obj-get-gob/kern-obj-set-gob.
    /// </summary>
    public object? SchemeData { get; set; }
    
    /// <summary>
    /// Flags controlling save behavior.
    /// GOB_SAVECAR (1) means save the car of the pair, not just cdr.
    /// </summary>
    public int Flags { get; set; }
    
    /// <summary>
    /// Reference count for garbage collection coordination.
    /// </summary>
    public int RefCount { get; set; }
    
    // Flag constants matching Nazghul.
    public const int GOB_SAVECAR = 1 << 0;
    
    /// <summary>
    /// Create a new Gob with the given Scheme data.
    /// </summary>
    public Gob(object? schemeData = null)
    {
        SchemeData = schemeData;
        Flags = 0;
        RefCount = 1;
    }
    
    /// <summary>
    /// Increment the reference count.
    /// </summary>
    public void AddRef()
    {
        RefCount++;
    }
    
    /// <summary>
    /// Decrement the reference count.
    /// Returns true if the gob should be deleted (refcount hit zero).
    /// </summary>
    public bool Release()
    {
        RefCount--;
        return RefCount <= 0;
    }
    
    /// <summary>
    /// Get the data to serialize when saving.
    /// Matches Nazghul's gob_save() logic:
    /// - If GOB_SAVECAR is set, save the entire SchemeData
    /// - Otherwise, save car(cdr(SchemeData))
    /// </summary>
    public object? GetDataForSave()
    {
        if (SchemeData == null)
            return null;
            
        if ((Flags & GOB_SAVECAR) != 0)
        {
            return SchemeData;
        }
        
        // For gobs without SAVECAR, the car is often a kernel object pointer
        // that shouldn't be serialized. We save car(cdr(data)) instead.
        // This requires the data to be a cons pair.
        if (SchemeData is IronScheme.Runtime.Cons cons)
        {
            var cdr = cons.cdr;
            if (cdr is IronScheme.Runtime.Cons cdrCons)
            {
                return cdrCons.car;
            }
        }
        
        // Fallback: just return the whole thing.
        return SchemeData;
    }
}