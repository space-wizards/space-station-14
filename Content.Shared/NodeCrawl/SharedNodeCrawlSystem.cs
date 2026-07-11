using Content.Shared.DoAfter;
using Content.Shared.Eye;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.RatKing;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.NodeCrawl;

/// <summary>
/// Manages entry & exit of node crawlers into node networks
/// </summary>
public abstract partial class SharedNodeCrawlSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedEyeSystem _eye = default!;
    [Dependency] private NodeCrawlerMovementSystem _nodeCrawler = default!;
    [Dependency] private EntityLookupSystem _entityLookup = default!;

    private const string MoverContainer = "mover-container";
    private static readonly EntProtoId MoverProto = "NodeCrawlMover";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrawlableNodeComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<NodeCrawlerComponent, NodeCrawlEnterDoAfterEvent>(OnNodeCrawlEntryDoAfter);
        SubscribeLocalEvent<NodeCrawlerComponent, NodeCrawlerArrivedAtNodeEvent>(OnArrivedAtNode);
        SubscribeLocalEvent<NodeCrawlerComponent, GetVisMaskEvent>(OnGetVisMask);

        SubscribeLocalEvent<CrawlableNodeComponent, ComponentShutdown>(OnCrawlableShutdown);
        SubscribeLocalEvent<NodeCrawlerMovementComponent, ComponentShutdown>(OnMovementShutdown);
        SubscribeLocalEvent<NodeCrawlerComponent, ComponentShutdown>(OnCrawlerShutdown);

        SubscribeLocalEvent<CrawlableNodeComponent, AnchorStateChangedEvent>(OnCrawlableAnchorChanged);

        SubscribeLocalEvent<RatKingComponent, NodeCrawlerStartedCrawlingEvent>(OnRatKingStartedCrawling);
    }

    private void OnGetVerbs(Entity<CrawlableNodeComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;
        var target = args.Target;
        if (!TryComp<NodeCrawlerComponent>(user, out var nodeCrawler))
            return;

        if (!_entityWhitelist.IsWhitelistPass(nodeCrawler.ExitNodes, target))
            return;

        if (!args.CanAccess)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => StartEntryDoAfter((user, nodeCrawler), target),
            Text = Loc.GetString("node-crawl-enter", ("target", target)),
        });
    }

    private void StartEntryDoAfter(Entity<NodeCrawlerComponent> ent, EntityUid target)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager,
            ent.Owner,
            ent.Comp.EnterDelay,
            new NodeCrawlEnterDoAfterEvent(),
            ent.Owner,
            target)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnNodeCrawlEntryDoAfter(Entity<NodeCrawlerComponent> ent, ref NodeCrawlEnterDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not { } target)
            return;

        NodeCrawl(ent, target);
    }

    private void NodeCrawl(Entity<NodeCrawlerComponent> ent, EntityUid target)
    {
        if (!_net.IsServer)
            return;

        var mover = Spawn(MoverProto, Transform(target).Coordinates);
        var crawler = Comp<NodeCrawlerMovementComponent>(mover);

        var container = _container.GetContainer(mover, MoverContainer);
        _container.Insert(ent.Owner, container);

        ent.Comp.Mover = mover;
        Dirty(ent);

        var evt = new NodeCrawlerStartedCrawlingEvent((mover, crawler));
        RaiseLocalEvent(ent, ref evt);

        _nodeCrawler.SetNode((mover, crawler), target);
        _nodeCrawler.SetHeldCrawler((mover, crawler), ent);

        SetupAir((mover, crawler));

        _mover.SetRelay(ent, mover);
        _physics.SetCanCollide(ent.Owner, false);
        _physics.SetCanCollide(mover, false);
        _eye.RefreshVisibilityMask(ent.Owner);
    }

    protected virtual void SetupAir(Entity<NodeCrawlerMovementComponent> movement)
    {
    }

    protected virtual void EjectAir(Entity<NodeCrawlerMovementComponent> movement)
    {
    }

    /// <summary>
    /// Causes this node crawler to exit its node crawl.
    /// </summary>
    /// <param name="ent">The crawler to exit node-crawl from.</param>
    public void ExitNodeCrawl(Entity<NodeCrawlerComponent> ent)
    {
        if (ent.Comp.Mover is not { } mover)
            return;

        ent.Comp.Mover = null;
        Dirty(ent);

        var container = _container.GetContainer(mover, MoverContainer);
        _container.Remove(ent.Owner, container);

        foreach (var other in _container.EmptyContainer(container))
        {
            if (!TryComp<NodeCrawlerComponent>(other, out var otherCrawler))
                continue;

            otherCrawler.Mover = null;
            Dirty(other, otherCrawler);
        }

        RemComp<RelayInputMoverComponent>(ent);
        if (_net.IsServer && !TerminatingOrDeleted(mover))
        {
            if (TryComp<NodeCrawlerMovementComponent>(mover, out var movement))
                EjectAir((mover, movement));

            QueueDel(mover); // deletion isn't predicted because client queued deletion doesn't interact well with container stuff
        }

        _physics.SetCanCollide(ent.Owner, true);
        _eye.RefreshVisibilityMask(ent.Owner);
    }

    private void OnArrivedAtNode(Entity<NodeCrawlerComponent> ent, ref NodeCrawlerArrivedAtNodeEvent args)
    {
        if (!_entityWhitelist.IsWhitelistPass(ent.Comp.ExitNodes, args.Node))
            return;

        ExitNodeCrawl(ent);
    }

    private void OnGetVisMask(Entity<NodeCrawlerComponent> ent, ref GetVisMaskEvent args)
    {
        if (ent.Comp.Mover is null)
            return;

        args.VisibilityMask |= (int)VisibilityFlags.Subfloor;
    }

    private void OnCrawlableShutdown(Entity<CrawlableNodeComponent> ent, ref ComponentShutdown args)
    {
        foreach (var crawler in ent.Comp.Crawlers)
        {
            var movement = Comp<NodeCrawlerMovementComponent>(crawler);
            if (movement.HeldCrawler is not { } held)
                continue;

            _nodeCrawler.SetNode((crawler, movement), null);
            ExitNodeCrawl((held, Comp<NodeCrawlerComponent>(held)));
        }
    }

    private void OnMovementShutdown(Entity<NodeCrawlerMovementComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Node is { } node)
        {
            var nodeComp = Comp<CrawlableNodeComponent>(node);
            nodeComp.Crawlers.Remove(ent);
            Dirty(node, nodeComp);
        }

        if (ent.Comp.HeldCrawler is { } crawler && !TerminatingOrDeleted(crawler) && TryComp<NodeCrawlerComponent>(crawler, out var nodeCrawler))
        {
            ExitNodeCrawl((crawler, nodeCrawler));
        }
    }

    private void OnCrawlerShutdown(Entity<NodeCrawlerComponent> ent, ref ComponentShutdown args)
    {
        ExitNodeCrawl(ent);
    }

    private void OnCrawlableAnchorChanged(Entity<CrawlableNodeComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            return;

        foreach (var crawler in ent.Comp.Crawlers)
        {
            var movement = Comp<NodeCrawlerMovementComponent>(crawler);
            if (movement.HeldCrawler is not { } held)
                continue;

            ExitNodeCrawl((held, Comp<NodeCrawlerComponent>(held)));
        }
    }

    private void OnRatKingStartedCrawling(Entity<RatKingComponent> ent, ref NodeCrawlerStartedCrawlingEvent args)
    {
        var entities = new HashSet<Entity<RatKingServantComponent>>();
        _entityLookup.GetEntitiesInRange(Transform(ent).Coordinates,
            ent.Comp.VentCrawlRecruitRadius,
            entities);

        var container = _container.GetContainer(args.Mover, MoverContainer);

        foreach (var servant in entities)
        {
            if (servant.Comp.King != ent)
                continue;

            _container.Insert(servant.Owner, container);
        }
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
        Dirty(ent);
    }
}
