using System.Linq;
using Content.Shared.Conditions.Satisfier;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IAllTagsCondition : ICondition<IAllTagsCondition>, IConditionWithDefaultSatisfactionRule
{
    ProtoId<TagPrototype>[] Tags { get; }

    Satisfier.Satisfier IConditionWithDefaultSatisfactionRule.GetDefaultSatisfier()
    {
        return new WithThreshold
        {
            Comparison = WithThreshold.Comparator.Equal,
            Threshold = 1f,
        };
    }
}

/// <summary>
/// Returns true if this entity has all the listed tags.
/// </summary>
public sealed partial class HasAllTagsEntityConditionSystem : EntitySystem
{
    [Dependency] private TagSystem _tag = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<TagComponent> entity, ref ConditionEvaluationEvent<IAllTagsCondition> args)
    {
        args.Handled = true;
        args.Value = args.Condition.Tags.Count(tag => _tag.HasTag(entity.Comp, tag));
    }
}
