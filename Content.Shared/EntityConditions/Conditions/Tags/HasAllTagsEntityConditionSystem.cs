﻿using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Tags;

/// <summary>
/// Returns true if we have all the required tags. False if we do not.
/// </summary>
public sealed partial class HasAllTagsEntityConditionSystem : EntityConditionSystem<TagComponent, HasAllTags>
{
    [Dependency] private readonly TagSystem _tag = default!;

    protected override void Condition(Entity<TagComponent> entity, ref EntityConditionEvent<HasAllTags> args)
    {
        args.Result = _tag.HasAllTags(entity.Comp, args.Condition.Tags);
    }
}

public sealed partial class HasAllTags : EntityConditionBase<HasAllTags>
{
    [DataField(required: true)]
    public ProtoId<TagPrototype>[] Tags = [];

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) =>
        Loc.GetString("reagent-effect-condition-guidebook-has-tag", ("tag", Tags), ("invert", Inverted));
}
