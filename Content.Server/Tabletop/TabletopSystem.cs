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
    [Dependency] private readonly EyeSystem _eye = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriberSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<TabletopStopPlayingEvent>(OnStopPlaying);

        SubscribeAllEvent<TabletopMoveEvent>(OnTabletopMove);

        SubscribeLocalEvent<TabletopGameComponent, ActivateInWorldEvent>(OnTabletopActivate);
        SubscribeLocalEvent<TabletopGameComponent, ComponentShutdown>(OnGameShutdown);
        SubscribeLocalEvent<TabletopGamerComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<TabletopGamerComponent, ComponentShutdown>(OnGamerShutdown);

        InitializeMap();
    }

    protected override void OnTabletopMove(TabletopMoveEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not { } playerSession)
            return;

        if (!TryComp(GetEntity(msg.TableUid), out TabletopGameComponent? tabletop) || tabletop.Session is not { } session)
            return;

        // Check if player is actually playing at this table.
        if (!session.Players.ContainsKey(playerSession))
            return;

        base.OnTabletopMove(msg, args);
    }

    private void OnTabletopActivate(EntityUid uid, TabletopGameComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        // Check that a player is attached to the entity.
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        OpenSessionFor(actor.PlayerSession, uid);
    }

    private void OnGameShutdown(EntityUid uid, TabletopGameComponent component, ComponentShutdown args)
    {
        CleanupSession(uid);
    }

    private void OnStopPlaying(TabletopStopPlayingEvent msg, EntitySessionEventArgs args)
    {
        CloseSessionFor(args.SenderSession, GetEntity(msg.TableUid));
    }

    private void OnPlayerDetached(EntityUid uid, TabletopGamerComponent component, PlayerDetachedEvent args)
    {
        if (component.Tabletop.IsValid())
            CloseSessionFor(args.Player, component.Tabletop);
    }

    private void OnGamerShutdown(EntityUid uid, TabletopGamerComponent component, ComponentShutdown args)
    {
        if (!TryComp(uid, out ActorComponent? actor))
            return;

        if (component.Tabletop.IsValid())
            CloseSessionFor(actor.PlayerSession, component.Tabletop);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TabletopGamerComponent>();
        while (query.MoveNext(out var uid, out var gamer))
        {
            if (!Exists(gamer.Tabletop))
                continue;

            if (!TryComp(uid, out ActorComponent? actor))
            {
                RemComp<TabletopGamerComponent>(uid);
                return;
            }

            if (actor.PlayerSession.Status != SessionStatus.InGame || !CanSeeTable(uid, gamer.Tabletop))
                CloseSessionFor(actor.PlayerSession, gamer.Tabletop);
        }
    }
}
