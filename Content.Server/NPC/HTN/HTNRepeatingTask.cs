using Content.Server.NPC.HTN.Preconditions;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.HTN;

/// <summary>
/// Repeats the tasks continuously until any of them fails.
/// </summary>
[Prototype("htnRepeating")]
public sealed class HTNRepeatingTask : HTNTask, IHTNCompound
{
    [DataField("preconditions")] public List<HTNPrecondition> Preconditions { get; } = new();

    [DataField("tasks", required: true)]
    public List<HTNTask> Tasks = default!;
}
