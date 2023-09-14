using Content.Shared.Construction.Components;
using Content.Shared.Examine;
using JetBrains.Annotations;

namespace Content.Shared.Construction.Steps;

[DataDefinition]
public sealed partial class PartAssemblyConstructionGraphStep : ConstructionGraphStep
{
    /// <summary>
    /// A valid ID on <see cref="PartAssemblyComponent"/>'s dictionary of strings to part lists.
    /// </summary>
    [DataField("assemblyId")]
    public string AssemblyId = string.Empty;

    /// <summary>
    /// A localization string used for
    /// </summary>
    [DataField("guideString")]
    public string GuideString = "construction-guide-condition-part-assembly";

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
