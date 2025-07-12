using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;

namespace Content.Shared.Movement.Systems;

/// <summary>
/// This handles the worm component
/// </summary>
public sealed class WormSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WormComponent, SharedStunSystem.StandUpAttemptEvent>(OnStandAttempt);
        SubscribeLocalEvent<WormComponent, SharedStunSystem.KnockedDownRefreshEvent>(OnKnockedDownRefresh);
        SubscribeLocalEvent<WormComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<WormComponent> ent, ref MapInitEvent args)
    {
        EnsureComp<KnockedDownComponent>(ent);
    }

    private void OnStandAttempt(Entity<WormComponent> ent, ref SharedStunSystem.StandUpAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        args.Cancelled = true;
        _popup.PopupClient(Loc.GetString("worm-component-stand-attempt"), ent, ent, PopupType.SmallCaution);
    }

    private void OnKnockedDownRefresh(Entity<WormComponent> ent, ref SharedStunSystem.KnockedDownRefreshEvent args)
    {
        args.FrictionModifier *= ent.Comp.FrictionModifier;
        args.SpeedModifier *= ent.Comp.SpeedModifier;
    }
}
