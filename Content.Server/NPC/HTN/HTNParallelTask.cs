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
public sealed class HTNParallelTask : HTNTask
{
    [DataField("tasks", required: true, customTypeSerializer:typeof(HTNTaskListSerializer))]
    public List<string> Tasks = new();
}
