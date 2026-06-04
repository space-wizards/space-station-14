using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Mind.Components;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Teleportation;

public sealed partial class PortalSystem : SharedPortalSystem
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedViewSubscriberSystem _viewSubscriber = default!;

    [Dependency] private EntityQuery<PortalComponent> _portalQuery = default!;
    [Dependency] private EntityQuery<MindContainerComponent> _mindContainerQuery = default!;
    [Dependency] private EntityQuery<GhostComponent> _ghostQuery = default!;

    private readonly Dictionary<ICommonSession, HashSet<EntityUid>> _subscribedViews = new();
    private readonly Dictionary<ICommonSession, HashSet<EntityUid>> _desiredViews = new();
    private readonly List<(EntityUid Portal, LinkedEntityComponent Link, float Distance)> _candidatePortals = new();
    private readonly List<ICommonSession> _sessionsToRemove = new();
    private readonly List<EntityUid> _viewsToRemove = new();
    private int _maxPreloadedPortals;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_config, CCVars.PortalMaxPreloaded, value => _maxPreloadedPortals = Math.Max(0, value), true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateLinkedViewSubscribers();
    }

    /// <summary>
    /// Handles preloading entities through portals so there's less pop-in.
    /// </summary>
    private void UpdateLinkedViewSubscribers()
    {
        foreach (var desiredViews in _desiredViews.Values)
        {
            desiredViews.Clear();
        }

        if (_maxPreloadedPortals > 0)
        {
            var actorQuery = EntityQueryEnumerator<ActorComponent, TransformComponent>();
            while (actorQuery.MoveNext(out var actorUid, out var actor, out var actorXform))
            {
                AddDesiredViewsForPlayer(actor.PlayerSession, actorUid, actorXform);
            }
        }

        _sessionsToRemove.Clear();
        foreach (var (session, subscribedViews) in _subscribedViews)
        {
            _desiredViews.TryGetValue(session, out var desiredViews);

            _viewsToRemove.Clear();
            foreach (var view in subscribedViews)
            {
                if (desiredViews?.Contains(view) == true)
                    continue;

                _viewsToRemove.Add(view);
            }

            foreach (var view in _viewsToRemove)
            {
                _viewSubscriber.RemoveViewSubscriber(view, session);
                subscribedViews.Remove(view);
            }

            if (subscribedViews.Count == 0)
                _sessionsToRemove.Add(session);
        }

        foreach (var session in _sessionsToRemove)
            _subscribedViews.Remove(session);

        foreach (var (session, desiredViews) in _desiredViews)
        {
            if (desiredViews.Count == 0)
                continue;

            var subscribedViews = GetSubscribedViews(session);

            foreach (var view in desiredViews)
            {
                if (!subscribedViews.Add(view))
                    continue;

                _viewSubscriber.AddViewSubscriber(view, session);
            }
        }
    }

    private void AddDesiredViewsForPlayer(ICommonSession session, EntityUid player, TransformComponent playerXform)
    {
        _candidatePortals.Clear();
        var playerCoords = _transform.GetMapCoordinates(player, playerXform);

        var portalQuery = EntityQueryEnumerator<PortalComponent, LinkedEntityComponent, TransformComponent>();
        while (portalQuery.MoveNext(out var portalUid, out var portal, out var link, out var portalXform))
        {
            if (portal.LinkedViewSubscriptionRange <= 0f || link.LinkedEntities.Count == 0)
                continue;

            var portalCoords = _transform.GetMapCoordinates(portalUid, portalXform);
            if (portalCoords.MapId != playerCoords.MapId)
                continue;

            var distance = (portalCoords.Position - playerCoords.Position).Length();
            if (distance > portal.LinkedViewSubscriptionRange)
                continue;

            _candidatePortals.Add((portalUid, link, distance));
        }

        if (_candidatePortals.Count == 0)
            return;

        _candidatePortals.Sort(static (a, b) => a.Distance.CompareTo(b.Distance));
        var views = GetDesiredViews(session);

        for (var i = 0; i < Math.Min(_maxPreloadedPortals, _candidatePortals.Count); i++)
        {
            var (portal, link, _) = _candidatePortals[i];
            views.Add(portal);

            foreach (var linked in link.LinkedEntities)
            {
                if (_portalQuery.HasComp(linked))
                    views.Add(linked);
            }
        }
    }

    private HashSet<EntityUid> GetDesiredViews(ICommonSession session)
    {
        if (_desiredViews.TryGetValue(session, out var views))
            return views;

        views = new HashSet<EntityUid>();
        _desiredViews.Add(session, views);
        return views;
    }

    private HashSet<EntityUid> GetSubscribedViews(ICommonSession session)
    {
        if (_subscribedViews.TryGetValue(session, out var views))
            return views;

        views = new HashSet<EntityUid>();
        _subscribedViews.Add(session, views);
        return views;
    }

    protected override void LogTeleport(EntityUid portal, EntityUid subject, EntityCoordinates source,
        EntityCoordinates target)
    {
        if (_mindContainerQuery.HasComp(subject) && !_ghostQuery.HasComp(subject))
            _adminLogger.Add(LogType.Teleport, LogImpact.Low, $"{ToPrettyString(subject):player} teleported via {ToPrettyString(portal)} from {source} to {target}");
    }
}
