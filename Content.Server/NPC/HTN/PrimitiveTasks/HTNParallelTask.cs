using Content.Server.NPC.HTN.Preconditions;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.HTN.PrimitiveTasks;

/// <summary>
/// Runs multiple HTN tasks in parallel.
/// </summary>
[Prototype("htnParallel")]
public sealed class HTNParallelTask : HTNTask
{
    /// <summary>
    /// What needs to be true for this task to be able to run.
    /// The operator may also implement its own checks internally as well if every primitive task using it requires it.
    /// </summary>
    [DataField("preconditions")] public List<HTNPrecondition> Preconditions = new();

    /// <summary>
    /// Tasks to be run in parallel.
    /// </summary>
    [DataField("tasks", required: true, customTypeSerializer: typeof(HTNTaskListSerializer))]
    public List<string> Tasks = new();
}
