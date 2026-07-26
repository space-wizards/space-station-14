using JetBrains.Annotations;
using Content.Shared.Interaction;
using Content.Shared.Tabletop;
using Content.Shared.Tabletop.Components;
using Content.Shared.Tabletop.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Server.Tabletop;

[UsedImplicitly]
public sealed partial class TabletopSystem : SharedTabletopSystem
{
    [Dependency] private EyeSystem _eye = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private ViewSubscriberSystem _viewSubscriberSystem = default!;

    #region Network Events
    [SubscribeNetworkEvent]
    private void OnStopPlaying(TabletopStopPlayingEvent msg, EntitySessionEventArgs args)
    {
        CloseSessionFor(args.SenderSession, GetEntity(msg.TableUid));
    }
    #endregion Network Events

    #region Local Events
    protected override void OnTabletopMove(TabletopMoveEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not { } playerSession)
            return;

        if (!GameQuery.TryComp(GetEntity(msg.TableUid), out TabletopGameComponent? tabletop) || !tabletop.HasSession)
            return;

        // Check if player is actually playing at this table.
        if (!tabletop.Players.ContainsKey(playerSession))
            return;

        base.OnTabletopMove(msg, args);
    }

    [SubscribeLocalEvent]
    private void OnTabletopActivate(Entity<TabletopGameComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        // Check that a player is attached to the entity.
        if (!ActorQuery.TryComp(args.User, out ActorComponent? actor))
            return;

        OpenSessionFor(actor.PlayerSession, ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnGameShutdown(Entity<TabletopGameComponent> ent, ref ComponentShutdown args)
    {
        CleanupSession(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(Entity<TabletopGamerComponent> ent, ref PlayerDetachedEvent args)
    {
        if (ent.Comp.Tabletop.IsValid())
            CloseSessionFor(args.Player, ent.Comp.Tabletop);
    }

    [SubscribeLocalEvent]
    private void OnGamerShutdown(Entity<TabletopGamerComponent> ent, ref ComponentShutdown args)
    {
        if (!ActorQuery.TryComp(ent.Owner, out ActorComponent? actor))
            return;

        if (ent.Comp.Tabletop.IsValid())
            CloseSessionFor(actor.PlayerSession, ent.Comp.Tabletop);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TabletopGamerComponent>();
        while (query.MoveNext(out var uid, out var gamer))
        {
            if (!Exists(gamer.Tabletop))
                continue;

            if (!ActorQuery.TryComp(uid, out ActorComponent? actor))
            {
                RemCompDeferred<TabletopGamerComponent>(uid);
                continue;
            }

            if (actor.PlayerSession.Status != SessionStatus.InGame || !CanSeeTable(uid, gamer.Tabletop))
                CloseSessionFor(actor.PlayerSession, gamer.Tabletop);
        }
    }
    #endregion Local Events
}
