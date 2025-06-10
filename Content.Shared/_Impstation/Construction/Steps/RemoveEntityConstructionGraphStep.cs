using Content.Shared.Construction;
using Content.Shared.Construction.Steps;
using Content.Shared.Examine;
using Content.Shared.Tag;

namespace Content.Shared._Impstation.Construction.Steps;

[DataDefinition]
public sealed partial class EntityRemoveConstructionGraphStep : ConstructionGraphStep
{
    /// <summary>
    /// A tag of the item you want to remove.
    /// </summary>
    [DataField("remove")]
    private string? _tag;

    /// <summary>
    /// A string representing the '$name' variable of the Loc file. By default, it's "Next, remove {$name}".
    /// </summary>
    [DataField]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// A localization string used when examining and for the guidebook.
    /// </summary>
    [DataField]
    public LocId GuideString = "construction-remove-arbitrary-entity";

    public bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory)
    {
        var tagSystem = entityManager.EntitySysManager.GetEntitySystem<TagSystem>();
        return !string.IsNullOrEmpty(_tag) && tagSystem.HasTag(uid, _tag);
    }

    public override void DoExamine(ExaminedEvent args)
    {
        if (string.IsNullOrEmpty(Name))
            return;
        args.PushMarkup(Loc.GetString(GuideString, ("name", Name)));
    }

    public override ConstructionGuideEntry GenerateGuideEntry()
    {
        return new ConstructionGuideEntry
        {
            Localization = "arbitrary-remove-construction-graph-step",
            Arguments = new (string, object)[] { ("name", Name) },
        };
    }
}
