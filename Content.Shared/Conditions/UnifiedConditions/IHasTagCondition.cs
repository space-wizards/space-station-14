using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IHasTagCondition : ICondition
{
    /// <summary>
    /// Tag required to fulfill this condition.
    /// </summary>
    ProtoId<TagPrototype> Tag { get; }
}

/// <summary>
/// Returns true if this entity has the listed tag.
/// </summary>
public sealed partial class HasTagEntityConditionSystem : ConditionEvaluatorSystem<IHasTagCondition>
{
    [Dependency] private TagSystem _tag = default!;

    public override float Evaluate(IHasTagCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        return _tag.HasTag(entityUid, condition.Tag) ? 1 : 0;
    }
}
