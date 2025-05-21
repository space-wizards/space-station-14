using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Throwing;

public sealed partial class CatchableSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ThrownItemSystem _thrown = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<HandsComponent> _handsQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CatchableComponent, ThrowDoHitEvent>(OnDoHit);

        _handsQuery = GetEntityQuery<HandsComponent>();
    }

    private void OnDoHit(Entity<CatchableComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!_handsQuery.TryGetComponent(args.Target, out var handsComp))
            return; // don't do anything for walls etc

        if (_random.ProbPredicted(_timing, ent.Comp.CatchChance, seed: GetNetEntity(ent).Id)
            && _hands.TryPickupAnyHand(args.Target, ent.Owner, handsComp: handsComp, animate: false))
        {
            // we picked it up already but we still have to raise the throwing stop (but not the landing) events at the right time
            // otherwise it will raise the events for that later while still in your hand
            _thrown.StopThrow(ent.Owner, args.Component);

            // collisions don't work properly with PopupPredicted or PlayPredicted
            // so we make this server only
            if (_net.IsClient)
                return;

            var selfMessage = Loc.GetString("catchable-component-success-self", ("item", ent.Owner), ("catcher", Identity.Entity(args.Target, EntityManager)));
            var othersMessage = Loc.GetString("catchable-component-success-others", ("item", ent.Owner), ("catcher", Identity.Entity(args.Target, EntityManager)));
            _popup.PopupEntity(selfMessage, args.Target, args.Target);
            _popup.PopupEntity(othersMessage, args.Target, Filter.PvsExcept(args.Target), true);
            _audio.PlayPvs(ent.Comp.CatchSuccessSound, args.Target);
        }
        else if (_net.IsServer)
        {
            var selfMessage = Loc.GetString("catchable-component-fail-self", ("item", ent.Owner), ("catcher", Identity.Entity(args.Target, EntityManager)));
            var othersMessage = Loc.GetString("catchable-component-fail-others", ("item", ent.Owner), ("catcher", Identity.Entity(args.Target, EntityManager)));
            _popup.PopupEntity(selfMessage, args.Target, args.Target);
            _popup.PopupEntity(othersMessage, args.Target, Filter.PvsExcept(args.Target), true);
            _audio.PlayPvs(ent.Comp.CatchFailSound, args.Target);
        }
    }
}
