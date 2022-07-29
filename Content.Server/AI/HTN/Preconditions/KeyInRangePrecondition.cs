using Robust.Shared.Map;

namespace Content.Server.AI.HTN.Preconditions;

/// <summary>
/// Is the specified key within the specified range of us.
/// </summary>
public sealed class KeyInRangePrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [ViewVariables, DataField("key", required: true)] public string Key = default!;

    [ViewVariables, DataField("rangeKey", required: true)]
    public string RangeKey = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValueOrDefault<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent<TransformComponent>(owner, out var xform))
            return false;

        return xform.Coordinates.InRange(_entManager, blackboard.GetValueOrDefault<EntityCoordinates>(RangeKey),
            blackboard.GetValue<float>(RangeKey));
    }
}
