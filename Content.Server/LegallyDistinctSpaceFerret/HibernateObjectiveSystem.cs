using Content.Shared.LegallyDistinctSpaceFerret;
using Content.Shared.Objectives.Components;

namespace Content.Server.LegallyDistinctSpaceFerret;

public sealed class HibernateObjectiveSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HibernateConditionComponent, ObjectiveGetProgressEvent>(OnHibernateGetProgress);
    }

    private static void OnHibernateGetProgress(EntityUid uid, HibernateConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = comp.Hibernated ? 1.0f : 0.0f;
    }
}
