using System.Linq;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IHasAnyTagCondition : ICondition
{
    /// <summary>
    /// List of tags from which one must be matched.
    /// </summary>
    ProtoId<TagPrototype>[] Tags { get; set; }
}

/// <summary>
/// Returns true if this entity have any of the listed tags.
/// </summary>
public sealed partial class HasAnyTagEntityConditionSystem : ConditionEvaluatorSystem<IHasAnyTagCondition>
{
    [Dependency] private TagSystem _tag = default!;

    public override float Evaluate(IHasAnyTagCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (!TryComp(entityUid, out TagComponent? tagComponent))
            return 0;
        return condition.Tags.Count(tag => _tag.HasTag(tagComponent, tag)) /
               (float)condition.Tags.Length;
    }
}
