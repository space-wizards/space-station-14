using System.Linq;
using Content.Shared.Conditions.Satisfier;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IAllTagsCondition : ICondition, IConditionWithDefaultSatisfactionRule
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
public sealed partial class HasAllTagsEntityConditionSystem : ConditionEvaluatorSystem<IAllTagsCondition>
{
    [Dependency] private TagSystem _tag = default!;

    public override float Evaluate(IAllTagsCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (!TryComp<TagComponent>(entityUid, out var component))
            return 0;
        return condition.Tags.Count(tag => _tag.HasTag(component, tag));
    }
}
