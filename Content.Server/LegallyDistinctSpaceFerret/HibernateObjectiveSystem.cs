using Content.Server.Popups;
using Content.Shared.LegallyDistinctSpaceFerret;
using Content.Shared.Objectives.Components;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Shared.Audio;

namespace Content.Server.LegallyDistinctSpaceFerret;

public sealed class HibernateObjectiveSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HibernateConditionComponent, ObjectiveGetProgressEvent>(OnHibernateGetProgress);
        SubscribeLocalEvent<HibernateConditionComponent, EntityHibernateAttemptSuccessEvent> (OnHibernationSuccess);
    }

    private static void OnHibernateGetProgress(EntityUid uid, HibernateConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = comp.Hibernated ? 1.0f : 0.0f;
    }

    private void OnHibernationSuccess(EntityUid uid, HibernateConditionComponent comp, EntityHibernateAttemptSuccessEvent args)
    {
        _popup.PopupEntity(Loc.GetString(comp.SuccessMessage), uid, PopupType.Large);
        _audio.PlayPvs(new SoundPathSpecifier(comp.SuccessSfx), uid, AudioParams.Default.WithVolume(0.66f));
        comp.Hibernated = true;
    }
}
