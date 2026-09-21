using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IHasTagCondition : ICondition<IHasTagCondition>
{
    /// <summary>
    /// Tag required to fulfill this condition.
    /// </summary>

    ProtoId<TagPrototype> Tag { get; }
}

/// <summary>
/// Returns true if this entity has the listed tag.
/// </summary>
public sealed partial class HasTagEntityConditionSystem : EntitySystem
{
    [Dependency] private TagSystem _tag = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<TagComponent> entity, ref ConditionEvaluationEvent<IHasTagCondition> args)
    {
        args.Handled = true;
        args.Value = _tag.HasTag(entity.Comp, args.Condition.Tag) ? 1 : 0;
    }
}
