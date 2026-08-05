using Content.Client.SubFloor;
using Content.Shared.NodeCrawl;
using Content.Shared.SubFloor;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.NodeCrawl;

public sealed partial class NodeCrawlSystem : SharedNodeCrawlSystem
{
    private const string OutlineShaderId = "NodeCrawlOutline";

    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private NodeCrawlerMovementSystem _nodeCrawler = default!;
    [Dependency] private NodeCrawlCrawlerSystem _crawler = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private NodeCrawlPipeOverlay? _pipeOverlay;
    private HashSet<EntityUid>? _reachableNodes;

    public IReadOnlySet<EntityUid>? ReachableNodes => _reachableNodes;

    public override void Initialize()
    {
        var outlineShader = _prototypeManager.Index(new ProtoId<ShaderPrototype>(OutlineShaderId)).InstanceUnique();
        _pipeOverlay = new NodeCrawlPipeOverlay(EntityManager, this, outlineShader);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

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

        if (SameNodes(reachable))
            return;

        var old = _reachableNodes;
        _reachableNodes = reachable;
        QueueAppearanceUpdates(old);
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

        if (!TryComp<NodeCrawlerMovementComponent>(mover, out var movement) || movement.Node is not { } node)
            return null;

        var reachable = new HashSet<EntityUid> { node };
        var pending = new Queue<EntityUid>();
        pending.Enqueue(node);

        while (pending.TryDequeue(out var current))
        {
            if (!TryComp<CrawlableNodeComponent>(current, out var currentComponent))
                continue;

            foreach (var connected in currentComponent.ReachableNodes)
            {
                if (!_nodeCrawler.CanTraverseNode((mover, movement), current, connected))
                    continue;

                if (reachable.Add(connected))
                    pending.Enqueue(connected);
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
            if (TryComp<AppearanceComponent>(node, out var appearance))
                _appearance.QueueUpdate(node, appearance);
        }
    }

    private void QueueAppearanceUpdates(HashSet<EntityUid>? nodes)
    {
        if (nodes == null)
            return;

        foreach (var node in nodes)
        {
            if (TryComp<AppearanceComponent>(node, out var appearance))
                _appearance.QueueUpdate(node, appearance);
        }
    }

    private bool SameNodes(HashSet<EntityUid>? nodes)
    {
        if (_reachableNodes == null || nodes == null)
            return _reachableNodes == null && nodes == null;

        return _reachableNodes.SetEquals(nodes);
    }
}
