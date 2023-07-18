using Content.Server.NPC.HTN.Preconditions;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.HTN;

/// <summary>
/// Runs the specified tasks in parallel.
/// If one is a compound task will run the first specified task there.
/// </summary>
/// <remarks>
/// Ends when either task completes or fails.
/// </remarks>
[Prototype("htnParallel")]
public sealed class HTNParallelTask : HTNTask, IHTNCompound
{
    [DataField("preconditions")] public List<HTNPrecondition> Preconditions { get; } = new();

    [DataField("tasks", required: true)]
    public List<HTNTask> Tasks = new();
}
