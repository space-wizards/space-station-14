using Content.Shared.EntityEffects;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Tags;

/// <summary>
/// Returns true if we have the required tag. False if we do not.
/// </summary>
public sealed partial class HasTagEntityConditionSystem : EntityConditionSystem<TagComponent, HasTag>
{
    [Dependency] private readonly TagSystem _tag = default!;

    protected override void Condition(Entity<TagComponent> entity, ref EntityConditionEvent<HasTag> args)
    {
        args.Result = _tag.HasTag(entity.Comp, args.Condition.Tag);
    }
}

public sealed class HasTag : EntityConditionBase<HasTag>
{
    [DataField(required: true)]
    public ProtoId<TagPrototype> Tag;
}
