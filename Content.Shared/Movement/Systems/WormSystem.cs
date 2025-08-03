using Content.Shared.Alert;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;

namespace Content.Shared.Movement.Systems;

/// <summary>
/// This handles the worm component
/// </summary>
public sealed class WormSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WormComponent, StandUpAttemptEvent>(OnStandAttempt);
        SubscribeLocalEvent<WormComponent, KnockedDownRefreshEvent>(OnKnockedDownRefresh);
        SubscribeLocalEvent<WormComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<WormComponent> ent, ref MapInitEvent args)
    {
        EnsureComp<KnockedDownComponent>(ent, out var knocked);
        _alerts.ShowAlert(ent, SharedStunSystem.KnockdownAlert);
        _stun.ToggleAutoStand((ent, knocked));
    }

    private void OnStandAttempt(Entity<WormComponent> ent, ref StandUpAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        args.Cancelled = true;
        _stun.ToggleAutoStand(ent.Owner);
        _popup.PopupClient(Loc.GetString("worm-component-stand-attempt"), ent, ent, PopupType.SmallCaution);
    }

    private void OnKnockedDownRefresh(Entity<WormComponent> ent, ref KnockedDownRefreshEvent args)
    {
        args.FrictionModifier *= ent.Comp.FrictionModifier;
        args.SpeedModifier *= ent.Comp.SpeedModifier;
    }
}
