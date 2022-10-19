using Content.Server.NPC.HTN.Preconditions;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.HTN.PrimitiveTasks;

[Prototype("htnPrimitive")]
public readonly record struct HTNPrimitiveTask : IHTNTask
{
    [IdDataField] public string ID { get; } = default!;

    /// <summary>
    /// Should we re-apply our blackboard state as a result of our operator during startup?
    /// This means you can re-use old data, e.g. re-using a pathfinder result, and avoid potentially expensive operations.
    /// </summary>
    [DataField("applyEffectsOnStartup")] public readonly bool ApplyEffectsOnStartup = true;

    /// <summary>
    /// What needs to be true for this task to be able to run.
    /// The operator may also implement its own checks internally as well if every primitive task using it requires it.
    /// </summary>
    [DataField("preconditions")] public readonly List<HTNPrecondition> Preconditions = new();

    [DataField("operator", required: true)]
    public readonly HTNOperator Operator = default!;
}
