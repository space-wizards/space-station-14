using Content.Shared.Localizations;
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

public sealed partial class HasAnyTag : EntityConditionBase<HasAnyTag>
{
    [DataField(required: true)]
    public ProtoId<TagPrototype>[] Tags = [];

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        var tagList = new List<string>();

        foreach (var type in Tags)
        {
            if (!prototype.Resolve(type, out var proto))
                continue;

            tagList.Add(proto.ID);
        }

        var names = ContentLocalizationManager.FormatListToOr(tagList);

        return Loc.GetString("reagent-effect-condition-guidebook-has-tag", ("tag", names), ("invert", Inverted));
    }
}
