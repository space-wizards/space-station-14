using Content.Shared._MACRO.TooShortForUI.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Climbing.Components;
using Content.Shared.Gravity;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Whitelist;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Shared._MACRO.TooShortForUI;

public sealed partial class TooShortForUI : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedGravitySystem _gravity = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TooShortForUIComponent, UserOpenActivatableUIAttemptEvent>(OnUIOpenAttempt);
    }

    public void OnUIOpenAttempt(Entity<TooShortForUIComponent> ent, ref UserOpenActivatableUIAttemptEvent args)
    {
        // first, check if we're buckled to something, and if we are, return
        if (TryComp<BuckleComponent>(ent, out var buckle) && buckle.Buckled)
            return;

        // next, check if we're in the air. if we are, return
        if (TryComp<PhysicsComponent>(ent, out var physics) && physics.BodyStatus != BodyStatus.OnGround)
            return;

        // next, check if we're weightless. you get the idea
        if (TryComp<GravityAffectedComponent>(ent, out var gravityAffected) && _gravity.IsWeightless((ent.Owner, gravityAffected)))
            return;

        // finally, check if we're on a table.
        if (TryComp<ClimbingComponent>(ent, out var climbing) && climbing.IsClimbing)
            return;

        // finally, if the target entity is on the whitelist, return if true
        if (_whitelist.IsWhitelistPass(ent.Comp.Whitelist, args.Target))
            return;

        // if the target entity is on the blacklist or no blacklist is defined, cancel the event
        if (_whitelist.IsWhitelistPassOrNull(ent.Comp.Blacklist, args.Target))
            args.Cancel();

        // if the event has been cancelled and there is popup text, popup
        if (args.Cancelled && ent.Comp.PopupText != null && _net.IsClient && _timing.IsFirstTimePredicted)
            _popup.PopupEntity(Loc.GetString(ent.Comp.PopupText), ent, ent);
    }
}
