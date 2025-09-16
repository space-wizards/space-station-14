using Content.Shared.EntityEffects;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Tags;

/// <summary>
/// Returns true if we have any of the required tags. False if we do not.
/// </summary>
public sealed partial class HasAnyTagEntityConditionSystem : EntityConditionSystem<TagComponent, HasAnyTag>
{
    [Dependency] private readonly TagSystem _tag = default!;

    protected override void Condition(Entity<TagComponent> entity, ref EntityConditionEvent<HasAnyTag> args)
    {
        args.Result = _tag.HasAnyTag(entity.Comp, args.Condition.Tags);
    }
}

public sealed class HasAnyTag : EntityConditionBase<HasAnyTag>
{
    [DataField(required: true)]
    public ProtoId<TagPrototype>[] Tags = [];
}
