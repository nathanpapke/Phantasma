using System;
using Phantasma;

namespace Phantasma.Models;

/// <summary>
/// Entry in the session's save registry.
/// Tracks an object and its save/load callbacks.
/// Mirrors Nazghul's data_obj_entry structure.
/// </summary>
internal readonly struct SaveEntry
{
    /// <summary>
    /// Game Object Being Tracked
    /// </summary>
    public object Object { get; }
    
    /// <summary>
    /// Destructor Callback - called when session is destroyed
    /// </summary>
    public Action<object>? Destructor { get; }
    
    /// <summary>
    /// Save Callback - called during session save
    /// </summary>
    public Action<object, SaveWriter>? SaveAction { get; }
    
    /// <summary>
    /// Post-load Initialization Callback - called after loading is complete
    /// </summary>
    public Action<object>? PostLoadAction { get; }
    
    public SaveEntry(object obj,
        Action<object>? destructor,
        Action<object, SaveWriter>? saveAction,
        Action<object>? postLoadAction)
    {
        Object = obj ?? throw new ArgumentNullException(nameof(obj));
        Destructor = destructor;
        SaveAction = saveAction;
        PostLoadAction = postLoadAction;
    }
}
