using Content.Shared.Disposal.Traversal;
using Content.Shared.Disposal.Unit;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Content.Shared.VentCrawl.Components;
using Content.Shared.Verbs;

namespace Content.Shared.VentCrawl;

public sealed partial class VentCrawlTubeSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private DisposalTraversalSystem _traversal = default!;
    [Dependency] private VentCrawlerSystem _ventCrawler = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VentCrawlEntryComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerb);
        SubscribeLocalEvent<EnterVentDoAfterEvent>(OnDoAfterEnterTube);
        SubscribeLocalEvent<ExitVentDoAfterEvent>(OnDoAfterExitTube);
    }

    private void AddAlternativeVerb(Entity<VentCrawlEntryComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!_ventCrawler.TryGetVentCrawler(args.User, out var crawler))
            return;

        AddExitVerb(ent, args.User, crawler.Comp, ref args);
        AddClimbedVerb(ent, args.User, crawler.Comp, ref args);
    }

    private void AddClimbedVerb(Entity<VentCrawlEntryComponent> ent, EntityUid user, VentCrawlerComponent crawler, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (TryComp<BeingDisposedComponent>(args.User, out var beingDisposed) &&
            HasComp<DisposalTraversalHolderComponent>(beingDisposed.Holder))
        {
            return;
        }

        var xform = Transform(ent);
        if (!xform.Anchored)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => TryEnter(ent, user, crawler),
            Text = Loc.GetString("comp-climbable-verb-climb")
        });
    }

    private void AddExitVerb(Entity<VentCrawlEntryComponent> ent, EntityUid user, VentCrawlerComponent crawler, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!TryComp<BeingDisposedComponent>(user, out var beingDisposed) ||
            !TryComp<DisposalTraversalHolderComponent>(beingDisposed.Holder, out var holder))
        {
            return;
        }

        if (holder.CurrentTube != ent.Owner)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => TryExit(ent, user, crawler),
            Text = Loc.GetString("vent-crawl-verb-exit")
        });
    }

    private void OnDoAfterEnterTube(EnterVentDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null || args.Args.Used == null)
            return;

        if (!TryComp<VentCrawlEntryComponent>(args.Args.Target.Value, out var entryComp))
            return;

        _traversal.Insert(args.Args.Target.Value, args.Args.Used.Value, entryComp.HolderPrototypeId);
        args.Handled = true;
    }

    private void OnDoAfterExitTube(ExitVentDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!TryComp<BeingDisposedComponent>(args.Args.User, out var beingDisposed))
            return;

        _traversal.ExitTraversal(beingDisposed.Holder);
        args.Handled = true;
    }

    /// <summary>
    /// Starts the do-after for entering a vent-crawl traversal entry.
    /// </summary>
    public void TryEnter(Entity<VentCrawlEntryComponent> entry, EntityUid user, VentCrawlerComponent crawler)
    {
        if (TryComp<WeldableComponent>(entry.Owner, out var weldable) && weldable.IsWelded)
        {
            _popup.PopupEntity(Loc.GetString("entity-storage-component-welded-shut-message"), user);
            return;
        }

        var args = new DoAfterArgs(EntityManager,
            user,
            crawler.EnterDelay,
            new EnterVentDoAfterEvent(),
            user,
            entry.Owner,
            user)
        {
            Broadcast = true,
            BreakOnMove = true,
            BreakOnDamage = true
        };

        _doAfter.TryStartDoAfter(args);
    }

    /// <summary>
    /// Starts the do-after for exiting a vent-crawl traversal entry.
    /// </summary>
    public void TryExit(Entity<VentCrawlEntryComponent> entry, EntityUid user, VentCrawlerComponent crawler)
    {
        if (TryComp<WeldableComponent>(entry.Owner, out var weldable) && weldable.IsWelded)
        {
            _popup.PopupEntity(Loc.GetString("entity-storage-component-welded-shut-message"), user);
            return;
        }

        var args = new DoAfterArgs(EntityManager,
            user,
            crawler.ExitDelay,
            new ExitVentDoAfterEvent(),
            user,
            entry.Owner,
            user)
        {
            Broadcast = true,
            DistanceThreshold = null,
            RequireCanInteract = false,
            BreakOnMove = true,
            BreakOnDamage = true,
            Hidden = true
        };

        _doAfter.TryStartDoAfter(args);
    }
}
