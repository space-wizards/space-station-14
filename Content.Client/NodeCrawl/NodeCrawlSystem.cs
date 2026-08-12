using Content.Client.SubFloor;
using Content.Shared.NodeCrawl;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.NodeCrawl;

public sealed partial class NodeCrawlSystem : SharedNodeCrawlSystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private NodeCrawlerMovementSystem _nodeCrawler = default!;
    [Dependency] private NodeCrawlCrawlerSystem _crawler = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    [Dependency] private EntityQuery<NodeCrawlerMovementComponent> _movementQuery;
    [Dependency] private EntityQuery<CrawlableNodeComponent> _crawlableQuery;
    [Dependency] private EntityQuery<AppearanceComponent> _appearanceQuery;

    private NodeCrawlPipeOverlay? _pipeOverlay;
    private HashSet<EntityUid>? _reachableNodes;
    private readonly Queue<EntityUid> _pendingNodes = new();

    public IReadOnlySet<EntityUid>? ReachableNodes => _reachableNodes;

    public override void Initialize()
    {
        base.Initialize();
        _pipeOverlay = new NodeCrawlPipeOverlay(EntityManager, this);
    }

    private void UpdateReachability()
    {
        var oldReachable = _reachableNodes;
        var reachable = GetLocalReachableNodes();
        QueueAppearanceUpdates(oldReachable);
        QueueAppearanceUpdates(reachable);
        if (_pipeOverlay != null && _overlayManager.HasOverlay<NodeCrawlPipeOverlay>() != (reachable != null))
        {
            if (reachable != null)
                _overlayManager.AddOverlay(_pipeOverlay);
            else
            {
                _overlayManager.RemoveOverlay(_pipeOverlay);
                _pipeOverlay.RemoveOutline();
            }
        }

        _reachableNodes = reachable;
    }

    [SubscribeLocalEvent]
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<ShaderPrototype>())
            _pipeOverlay?.SetShader(_prototypeManager.Index(NodeCrawlPipeOverlay.OutlineShader).InstanceUnique());
    }

    [SubscribeLocalEvent]
    private void OnStartedCrawling(Entity<NodeCrawlerComponent> ent, ref NodeCrawlerStartedCrawlingEvent args)
    {
        UpdateReachability();
    }

    [SubscribeLocalEvent]
    private void OnStoppedCrawling(Entity<NodeCrawlerComponent> ent, ref NodeCrawlerStoppedCrawlingEvent args)
    {
        UpdateReachability();
    }

    [SubscribeLocalEvent]
    private void OnAttached(Entity<NodeCrawlerComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        UpdateReachability();
    }

    [SubscribeLocalEvent]
    private void OnDetached(Entity<NodeCrawlerComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        QueueAppearanceUpdates(_reachableNodes);
        _reachableNodes = null;
        _pipeOverlay?.RemoveOutline();
    }

    [SubscribeLocalEvent]
    private void OnAfterAutoHandleState(Entity<NodeCrawlerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_player.LocalEntity is not { } player
            || !_crawler.TryGetNodeCrawler(player, out var crawler)
            || crawler.Owner != ent.Owner)
            return;

        UpdateReachability();
        UpdateSubfloor(ent.Comp.Mover is not null);
    }

    [SubscribeLocalEvent]
    private void OnMovementAfterAutoHandleState(Entity<NodeCrawlerMovementComponent> ent,
        ref AfterAutoHandleStateEvent args)
    {
        if (_player.LocalEntity is not { } player
            || !_crawler.TryGetNodeCrawler(player, out var crawler)
            || crawler.Comp.Mover != ent.Owner)
            return;

        UpdateReachability();
    }

    [SubscribeLocalEvent]
    private void OnCrawlableAfterAutoHandleState(Entity<CrawlableNodeComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateReachability();
    }

    [SubscribeLocalEvent]
    private void OnGetSubFloorReveal(Entity<CrawlableNodeComponent> ent, ref GetSubFloorRevealEvent args)
    {
        args.Revealed |= _reachableNodes?.Contains(ent.Owner) == true;
    }

    private HashSet<EntityUid>? GetLocalReachableNodes()
    {
        var local = _player.LocalEntity;
        if (local is not { } uid || !_crawler.TryGetNodeCrawler(uid, out var crawler)
            || crawler.Comp.Mover is not { } mover)
            return null;

        if (!_movementQuery.TryGetComponent(mover, out var movement) || movement.Node is not { } node)
            return null;

        var reachable = new HashSet<EntityUid>();
        _pendingNodes.Clear();
        reachable.Add(node);
        _pendingNodes.Enqueue(node);

        while (_pendingNodes.TryDequeue(out var current))
        {
            if (!_crawlableQuery.TryGetComponent(current, out var currentComponent))
                continue;

            foreach (var connected in currentComponent.ReachableNodes)
            {
                if (!_nodeCrawler.CanTraverseNode((mover, movement), current, connected))
                    continue;

                if (reachable.Add(connected))
                    _pendingNodes.Enqueue(connected);
            }
        }

        return reachable;
    }

    private void UpdateSubfloor(bool crawling)
    {
        if (!crawling || _player.LocalEntity is not { } local || !_crawler.HasNodeCrawler(local))
            return;

        foreach (var node in _reachableNodes ?? [])
        {
            if (_appearanceQuery.TryGetComponent(node, out var appearance))
                _appearance.QueueUpdate(node, appearance);
        }
    }

    private void QueueAppearanceUpdates(IReadOnlySet<EntityUid>? nodes)
    {
        if (nodes == null)
            return;

        foreach (var node in nodes)
        {
            if (_appearanceQuery.TryGetComponent(node, out var appearance))
                _appearance.QueueUpdate(node, appearance);
        }
    }
}
