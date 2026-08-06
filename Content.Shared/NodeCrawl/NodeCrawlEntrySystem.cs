using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;

namespace Content.Shared.NodeCrawl;

/// <summary>
/// Handles entry/exit verbs and do-after interactions for crawling into and out of node crawl vents.
/// </summary>
public sealed partial class NodeCrawlEntrySystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedNodeCrawlSystem _nodeCrawl = default!;
    [Dependency] private NodeCrawlCrawlerSystem _crawler = default!;
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private WeldableSystem _weldable = default!;

    [Dependency] private EntityQuery<NodeCrawlerMovementComponent> _movementQuery;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NodeCrawlVentAccessComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<NodeCrawlEnterDoAfterEvent>(OnEnter);
        SubscribeLocalEvent<NodeCrawlExitDoAfterEvent>(OnExit);
    }

    private void OnGetVerbs<T>(Entity<T> ent, ref GetVerbsEvent<AlternativeVerb> args) where T : Component
    {
        var user = args.User;
        if (!_crawler.TryGetNodeCrawler(user, out var crawler))
            return;

        if (!_entityWhitelist.IsWhitelistPass(crawler.Comp.EntranceNodes, ent.Owner))
            return;

        if (crawler.Comp.Mover is { } mover
            && _movementQuery.TryGetComponent(mover, out var movement) && movement.Node == ent.Owner)
        {
            args.Verbs.Add(new AlternativeVerb { Act = () => TryExit(ent.Owner, user, crawler.Comp.ExitDelay), Text = Loc.GetString("node-crawl-exit", ("target", ent.Owner)) }) ;
            return;
        }

        if (args.CanAccess && Transform(ent).Anchored)
            args.Verbs.Add(new AlternativeVerb { Act = () => TryEnter(ent.Owner, user, crawler.Comp.EnterDelay), Text = Loc.GetString("node-crawl-enter", ("target", ent.Owner)) });
    }

    private void TryEnter(EntityUid target, EntityUid user, TimeSpan delay)
    {
        if (_weldable.IsWelded(target))
        {
            _popup.PopupEntity(Loc.GetString("entity-storage-component-welded-shut-message"), user);
            return;
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, delay, new NodeCrawlEnterDoAfterEvent(), user, target, user)
        {
            Broadcast = true, BreakOnMove = true, BreakOnDamage = true
        });
    }

    private void TryExit(EntityUid target, EntityUid user, TimeSpan delay)
    {
        if (_weldable.IsWelded(target))
        {
            _popup.PopupEntity(Loc.GetString("entity-storage-component-welded-shut-message"), user);
            return;
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, delay, new NodeCrawlExitDoAfterEvent(), user, target, user)
        {
            Broadcast = true, DistanceThreshold = null, RequireCanInteract = false, BreakOnMove = true, BreakOnDamage = true, Hidden = true
        });
    }

    private void OnEnter(NodeCrawlEnterDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target is not { } target || args.Args.Used is not { } user)
            return;

        _nodeCrawl.EnterNodeCrawl(user, target);
        args.Handled = true;
    }

    private void OnExit(NodeCrawlExitDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        _nodeCrawl.ExitNodeCrawl(args.Args.User);
        args.Handled = true;
    }
}
