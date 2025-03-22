using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Shared.Silicons.Borgs;

public sealed class BrainUnborgableSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BrainUnborgableComponent, AttemptMakeBrainIntoSiliconEvent>(OnAttemptTurnBrainIntoSilicon);
    }

    private void OnAttemptTurnBrainIntoSilicon(Entity<BrainUnborgableComponent> entity, ref AttemptMakeBrainIntoSiliconEvent args)
    {
        _popupSystem.PopupPredicted(
            Loc.GetString(entity.Comp.FailureMessage, ("brain", entity), ("mmi", args.BrainHolder)),
            args.BrainHolder,
            entity);

        args.Cancel();
    }
}
