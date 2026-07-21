using Content.Shared.Examine;
using Content.Shared.Wall;
using JetBrains.Annotations;

namespace Content.Shared.Construction.Conditions;

/// <summary>
/// A condition to check that an entity has no important parented wallmounts on it.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class NoImportantParentedWallmounts : IGraphCondition
{
    public bool Condition(EntityUid uid, IEntityManager entMan)
    {
        var parentSys = entMan.System<ParentToWallSystem>();
        return !parentSys.HasImportantWallmounts(uid);
    }

    public bool DoExamine(ExaminedEvent args)
    {
        var entity = args.Examined;

        var parentSys = IoCManager.Resolve<IEntityManager>().System<ParentToWallSystem>();

        if (!parentSys.HasImportantWallmounts(entity))
            return false;

        args.PushMarkup(Loc.GetString("construction-examine-condition-no-important-parented"));
        return true;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield return new ConstructionGuideEntry()
        {
            Localization = "construction-step-condition-no-important-parented"
        };
    }
}
