namespace Phantasma.Models;

/// <summary>
/// Represents a scheduled Scheme procedure to be called at a specific tick.
/// </summary>
public class Job
{
    /// <summary>
    /// The absolute tick number when this job should execute.
    /// </summary>
    public int TargetTick { get; set; }
    
    /// <summary>
    /// The Scheme procedure/closure to invoke.
    /// </summary>
    public object Procedure { get; set; }
    
    /// <summary>
    /// The argument to pass to the procedure (e.g., the game object).
    /// </summary>
    public object Data { get; set; }
    
    /// <summary>
    /// For recurring jobs, the number of ticks between executions.
    /// 0 = one-shot job (most common).
    /// </summary>
    public int Period { get; set; }
    
    public Job(int targetTick, object procedure, object data, int period = 0)
    {
        TargetTick = targetTick;
        Procedure = procedure;
        Data = data;
        Period = period;
    }
}
