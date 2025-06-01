using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Shared.Silicons.Borgs;

public sealed class MMIIncompatibleSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MMIIncompatibleComponent, AttemptMakeBrainIntoSiliconEvent>(OnAttemptTurnBrainIntoSilicon);
    }

    private void OnAttemptTurnBrainIntoSilicon(Entity<MMIIncompatibleComponent> entity, ref AttemptMakeBrainIntoSiliconEvent args)
    {
        _popupSystem.PopupPredicted(
            Loc.GetString(entity.Comp.FailureMessage, ("brain", entity), ("mmi", args.BrainHolder)),
            args.BrainHolder,
            entity);

        args.Cancelled = true;
    }
}
