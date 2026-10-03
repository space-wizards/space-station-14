using System.Linq;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IHasAnyTagCondition : ICondition<IHasAnyTagCondition>
{
    /// <summary>
    /// List of tags from which one must be matched.
    /// </summary>
    ProtoId<TagPrototype>[] Tags { get; set; }
}

/// <summary>
/// Returns true if this entity have any of the listed tags.
/// </summary>
public sealed partial class HasAnyTagEntityConditionSystem : EntitySystem
{
    [Dependency] private TagSystem _tag = default!;

    private void Condition(Entity<TagComponent> entity, ref ConditionEvaluationEvent<IHasAnyTagCondition> args)
    {
        args.Handled = true;
        //count matches to scale, as default condition if value != 0 -> satisfy == true.
        args.Value = args.Condition.Tags.Count(tag => _tag.HasTag(entity.Comp, tag)) /
                     (float)args.Condition.Tags.Length;
    }
}
