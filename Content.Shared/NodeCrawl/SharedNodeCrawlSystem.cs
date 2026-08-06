using Content.Shared.Eye;
using Content.Shared.Movement.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.NodeCrawl;

/// <summary>
/// Manages entry & exit of node crawlers into node networks
/// </summary>
public abstract partial class SharedNodeCrawlSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedEyeSystem _eye = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private NodeCrawlerMovementSystem _nodeCrawler = default!;
    [Dependency] private NodeCrawlCrawlerSystem _crawler = default!;

    [Dependency] private EntityQuery<CrawlableNodeComponent> _crawlableQuery;
    [Dependency] private EntityQuery<NodeCrawlerComponent> _crawlerQuery;
    [Dependency] private EntityQuery<NodeCrawlerMovementComponent> _movementQuery;

    public static readonly string MoverContainer = "mover-container";

    [SubscribeLocalEvent]
    private void OnGetVisMask(Entity<NodeCrawlerComponent> ent, ref GetVisMaskEvent args)
    {
        if (ent.Comp.Mover is null)
            return;

        args.VisibilityMask |= (int)VisibilityFlags.Subfloor;
    }

    [SubscribeLocalEvent]
    private void OnCrawlableShutdown(Entity<CrawlableNodeComponent> ent, ref ComponentShutdown args)
    {
        var crawlers = new List<EntityUid>(ent.Comp.Crawlers);
        foreach (var crawler in crawlers)
        {
            var movement = Comp<NodeCrawlerMovementComponent>(crawler);
            if (movement.HeldCrawler is not { } held)
                continue;

            _nodeCrawler.SetNode((crawler, movement), null);
            ExitNodeCrawl(held);
        }
    }

    /// <summary>
    /// When the mover is being deleted (e.g., during prediction reset),
    /// remove contained entities to prevent them from being deleted recursively.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMovementTerminating(Entity<NodeCrawlerMovementComponent> ent, ref EntityTerminatingEvent args)
    {
        if (!_container.TryGetContainer(ent, MoverContainer, out var container))
            return;

        var containedList = new List<EntityUid>(container.ContainedEntities);
        foreach (var entity in containedList)
        {
            _container.Remove(entity, container, reparent: false, force: true);

            var xform = Transform(entity);
            if (xform.ParentUid == ent.Owner)
                _xform.AttachToGridOrMap(entity, xform);
        }
    }

    [SubscribeLocalEvent]
    private void OnMovementShutdown(Entity<NodeCrawlerMovementComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Node is { } node)
        {
            var nodeComp = Comp<CrawlableNodeComponent>(node);
            nodeComp.Crawlers.Remove(ent);
            DirtyField(node, nodeComp, nameof(CrawlableNodeComponent.Crawlers));
        }

        if (ent.Comp.HeldCrawler is { } crawler && !TerminatingOrDeleted(crawler))
        {
            ExitNodeCrawl(crawler);
        }
    }

    [SubscribeLocalEvent]
    private void OnCrawlerShutdown(Entity<NodeCrawlerComponent> ent, ref ComponentShutdown args)
    {
        ExitNodeCrawl(ent);
    }

    [SubscribeLocalEvent]
    private void OnCrawlableAnchorChanged(Entity<CrawlableNodeComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            return;

        var crawlers = new List<EntityUid>(ent.Comp.Crawlers);
        foreach (var crawler in crawlers)
        {
            var movement = Comp<NodeCrawlerMovementComponent>(crawler);
            if (movement.HeldCrawler is not { } held)
                continue;

            ExitNodeCrawl(held);
        }
    }

    /// <summary>
    /// Causes an entity to begin node crawling at the target entity.
    /// </summary>
    /// <param name="uid">The entity to node crawl.</param>
    /// <param name="target">The target to crawl into.</param>
    public void EnterNodeCrawl(EntityUid uid, EntityUid target)
    {
        if (!Exists(target) ||
            !_crawlableQuery.HasComponent(target) ||
            !_crawler.TryGetNodeCrawler(uid, out var ent, out var user) ||
            ent.Comp.Mover != null)
            return;

        var mover = PredictedSpawnAttachedTo(ent.Comp.MoverProto, Transform(target).Coordinates);
        var crawler = Comp<NodeCrawlerMovementComponent>(mover);

        var container = _container.GetContainer(mover, MoverContainer);
        _container.Insert(user, container);

        ent.Comp.Mover = mover;
        DirtyField(ent.AsNullable(), nameof(NodeCrawlerComponent.Mover));

        var ev = new NodeCrawlerStartedCrawlingEvent((mover, crawler));
        RaiseLocalEvent(user, ref ev);

        _nodeCrawler.SetNode((mover, crawler), target);
        _nodeCrawler.SetHeldCrawler((mover, crawler), user);

        SetupAir((mover, crawler));

        _mover.SetRelay(user, mover);
        _physics.SetCanCollide(mover, false);
        _eye.RefreshVisibilityMask(user);
    }

    /// <summary>
    /// Sets air for the <see cref="NodeCrawlerMovementComponent"/> entity, granting the contained entity a "safety bubble" containing air if the atmosphere is otherwise dangerous.
    /// </summary>
    protected virtual void SetupAir(Entity<NodeCrawlerMovementComponent> movement)
    {
    }

    /// <summary>
    /// Removes air from the <see cref="NodeCrawlerMovementComponent"/> entity and dumps it into the atmosphere at its location.
    /// </summary>
    protected virtual void EjectAir(Entity<NodeCrawlerMovementComponent> movement)
    {
    }

    /// <summary>
    /// Causes this node crawler to exit its node crawl.
    /// </summary>
    /// <param name="uid">The crawler to exit node-crawl from.</param>
    public void ExitNodeCrawl(EntityUid uid)
    {
        if (!_crawler.TryGetNodeCrawler(uid, out var ent, out var user))
            return;

        if (ent.Comp.Mover is not { } mover)
            return;

        ent.Comp.Mover = null;
        DirtyField(ent.AsNullable(), nameof(NodeCrawlerComponent.Mover));

        var container = _container.GetContainer(mover, MoverContainer);
        _container.Remove(user, container);

        foreach (var other in _container.EmptyContainer(container))
        {
            if (!_crawlerQuery.TryGetComponent(other, out var otherCrawler))
                continue;

            otherCrawler.Mover = null;
            DirtyField(other, otherCrawler, nameof(NodeCrawlerComponent.Mover));
        }

        _mover.RemoveRelay(user);
        if (!TerminatingOrDeleted(mover))
        {
            if (_movementQuery.TryGetComponent(mover, out var movement))
                EjectAir((mover, movement));

            PredictedDel(mover);
        }

        var ev = new NodeCrawlerStoppedCrawlingEvent();
        RaiseLocalEvent(user, ref ev);

        _physics.SetCanCollide(user, true);
        _eye.RefreshVisibilityMask(user);
    }

    /// <summary>
    /// Sets the enter delay for a node crawler entity.
    /// </summary>
    /// <param name="ent">The entity to set the delay for.</param>
    /// <param name="delay">The delay.</param>
    public void SetEnterDelay(Entity<NodeCrawlerComponent?> ent, TimeSpan delay)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.EnterDelay = delay;
        DirtyField(ent.AsNullable(), nameof(NodeCrawlerComponent.EnterDelay));
    }
}
