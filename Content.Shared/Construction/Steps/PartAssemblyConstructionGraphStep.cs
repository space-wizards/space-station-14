using Content.Shared.Construction.Components;
using Content.Shared.Examine;

namespace Content.Shared.Construction.Steps;

[DataDefinition]
public sealed partial class PartAssemblyConstructionGraphStep : ConstructionGraphStep
{
    /// <summary>
    /// A valid ID on <see cref="PartAssemblyComponent"/>'s dictionary of strings to part lists.
    /// </summary>
    [DataField]
    public string AssemblyId = string.Empty;

    /// <summary>
    /// A localization string used when examining and for the guidebook.
    /// </summary>
    [DataField]
    public LocId GuideString = "construction-guide-condition-part-assembly";

    public bool Condition(EntityUid uid, IEntityManager entityManager)
    {
        return entityManager.System<PartAssemblySystem>().IsAssemblyFinished(uid, AssemblyId);
    }

    public override void DoExamine(ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(GuideString));
    }

    public override ConstructionGuideEntry GenerateGuideEntry()
    {
        return new ConstructionGuideEntry
        {
            Localization = GuideString,
        };
    }
}
