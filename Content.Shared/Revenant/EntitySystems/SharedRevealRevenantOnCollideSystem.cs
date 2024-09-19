using Content.Shared.Revenant.Components;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Revenant.EntitySystems;

public abstract class SharedRevealRevenantOnCollideSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string CorporealStatusId = "Corporeal";
    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string StunStatusId = "Stun";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevealRevenantOnCollideComponent, StartCollideEvent>(OnCollideStart);
    }

    public void OnCollideStart(EntityUid uid, RevealRevenantOnCollideComponent comp, StartCollideEvent args)
    {
        if (!HasComp<RevenantComponent>(args.OtherEntity))
            return;

        if (!string.IsNullOrEmpty(comp.PopupText) && !_status.HasStatusEffect(args.OtherEntity, CorporealStatusId))
            _popup.PopupClient(
                Loc.GetString(comp.PopupText, ("revealer", uid), ("revenant", args.OtherEntity)),
                args.OtherEntity,
                args.OtherEntity
            );

        _status.TryAddStatusEffect<CorporealComponent>(args.OtherEntity, CorporealStatusId, comp.RevealTime, true);

        if (comp.StunTime != null && !_status.HasStatusEffect(args.OtherEntity, StunStatusId))
            _stun.TryStun(args.OtherEntity, comp.StunTime.Value, true);
    }
}