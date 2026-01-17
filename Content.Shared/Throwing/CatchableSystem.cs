using Content.Shared.CombatMode;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Throwing;

public sealed partial class CatchableSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ThrownItemSystem _thrown = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private EntityQuery<HandsComponent> _handsQuery;
    private EntityQuery<CombatModeComponent> _combatModeQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CatchableComponent, ThrowDoHitEvent>(OnDoHit);

        _handsQuery = GetEntityQuery<HandsComponent>();
        _combatModeQuery = GetEntityQuery<CombatModeComponent>();
    }

    private void OnDoHit(Entity<CatchableComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!_handsQuery.TryGetComponent(args.Target, out var handsComp))
            return; // don't do anything for walls etc

        // Is the catcher in combat mode if required?
        if (ent.Comp.RequireCombatMode && (!_combatModeQuery.TryComp(args.Target, out var combatModeComp) || !combatModeComp.IsInCombatMode))
            return;

        // Is the catcher able to catch this item?
        if (!_whitelist.IsWhitelistPassOrNull(ent.Comp.CatcherWhitelist, args.Target))
            return;

        var attemptEv = new CatchAttemptEvent(ent.Owner, ent.Comp.CatchChance);
        RaiseLocalEvent(args.Target, ref attemptEv);

        if (attemptEv.Cancelled)
            return;

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);
        if (!rand.Prob(ent.Comp.CatchChance))
            return;

        // Try to catch!
        if (!_hands.TryPickupAnyHand(args.Target, ent.Owner, handsComp: handsComp, animate: false))
            return; // The hands are full!

        // Success!

        // We picked it up already but we still have to raise the throwing stop (but not the landing) events at the right time,
        // otherwise it will raise the events for that later while still in your hand
        _thrown.StopThrow(ent.Owner, args.Component);

        // Collisions don't work properly with PopupPredicted or PlayPredicted.
        // So we make this server only.
        if (_net.IsClient)
            return;

        var selfMessage = Loc.GetString("catchable-component-success-self", ("item", ent.Owner), ("catcher", Identity.Entity(args.Target, EntityManager)));
        var othersMessage = Loc.GetString("catchable-component-success-others", ("item", ent.Owner), ("catcher", Identity.Entity(args.Target, EntityManager)));
        _popup.PopupEntity(selfMessage, args.Target, args.Target);
        _popup.PopupEntity(othersMessage, args.Target, Filter.PvsExcept(args.Target), true);
        _audio.PlayPvs(ent.Comp.CatchSuccessSound, args.Target);
    }
}
