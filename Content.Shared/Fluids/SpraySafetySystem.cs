using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Fluids.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Fluids;

public sealed class SpraySafetySystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpraySafetyComponent, SolutionTransferAttemptEvent>(OnTransferAttempt);
        SubscribeLocalEvent<SpraySafetyComponent, SolutionTransferredEvent>(OnTransferred);
        SubscribeLocalEvent<SpraySafetyComponent, SprayAttemptEvent>(OnSprayAttempt);
    }

    private void OnTransferAttempt(Entity<SpraySafetyComponent> ent, ref SolutionTransferAttemptEvent args)
    {
        var (uid, comp) = ent;
        if (uid == args.To && !_toggle.IsActivated(uid))
            args.Cancel(Loc.GetString(comp.Popup));
    }

    private void OnTransferred(Entity<SpraySafetyComponent> ent, ref SolutionTransferredEvent args)
    {
        _audio.PlayPredicted(ent.Comp.RefillSound, ent, args.User);
    }

    private void OnSprayAttempt(Entity<SpraySafetyComponent> ent, ref SprayAttemptEvent args)
    {
        if (!_toggle.IsActivated(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.Popup), ent, args.User);
            args.Cancel();
        }
    }
}
