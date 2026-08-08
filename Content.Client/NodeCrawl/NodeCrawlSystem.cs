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
        _prototypeManager.PrototypesReloaded += OnPrototypesReloaded;
        _pipeOverlay = new NodeCrawlPipeOverlay(EntityManager, this);
    }

    public override void Shutdown()
    {
        _prototypeManager.PrototypesReloaded -= OnPrototypesReloaded;
        base.Shutdown();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<ShaderPrototype>())
            _pipeOverlay?.SetShader(_prototypeManager.Index(NodeCrawlPipeOverlay.OutlineShader).InstanceUnique());
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        QueueAppearanceUpdates(_reachableNodes);
        var reachable = GetLocalReachableNodes();
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
        QueueAppearanceUpdates(_reachableNodes);
    }

    [SubscribeLocalEvent]
    private void OnAttached(Entity<NodeCrawlerComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        UpdateSubfloor(ent.Comp.Mover is not null);
    }

    [SubscribeLocalEvent]
    private void OnDetached(Entity<NodeCrawlerComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        var old = _reachableNodes;
        _reachableNodes = null;
        QueueAppearanceUpdates(old);
        _pipeOverlay?.RemoveOutline();
    }

    [SubscribeLocalEvent]
    private void OnAfterAutoHandleState(Entity<NodeCrawlerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_player.LocalEntity is not { } player
            || !_crawler.TryGetNodeCrawler(player, out var crawler)
            || crawler.Owner != ent.Owner)
            return;

        UpdateSubfloor(ent.Comp.Mover is not null);
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

        var reachable = _reachableNodes ??= [];
        reachable.Clear();
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

    private void QueueAppearanceUpdates(HashSet<EntityUid>? nodes)
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
