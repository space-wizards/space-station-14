using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Sticky.Components;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.Sticky.Systems;

public sealed class StickySystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private const string StickerSlotId = "stickers_container";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StickyComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<StickyComponent, StickyDoAfterEvent>(OnStickyDoAfter);
        SubscribeLocalEvent<StickyComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnAfterInteract(Entity<StickyComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not {} target)
            return;

        // try stick object to a clicked target entity
        args.Handled = StartSticking(ent, target, args.User);
    }

    private void OnGetVerbs(Entity<StickyComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        var (uid, comp) = ent;
        if (comp.StuckTo == null || !comp.CanUnstick || !args.CanInteract || args.Hands == null)
            return;

        // we can't use args.CanAccess, because it stuck in another container
        // we also need to ignore entity that it stuck to
        var user = args.User;
        var inRange = _interaction.InRangeUnobstructed(uid, user,
            predicate: entity => comp.StuckTo == entity);
        if (!inRange)
            return;

        args.Verbs.Add(new Verb
        {
            DoContactInteraction = true,
            Text = Loc.GetString(comp.VerbText),
            Icon = comp.VerbIcon,
            Act = () => StartUnsticking(ent, user)
        });
    }

    private bool StartSticking(Entity<StickyComponent> ent, EntityUid target, EntityUid user)
    {
        var (uid, comp) = ent;

        // check whitelist and blacklist
        if (_whitelist.IsWhitelistFail(comp.Whitelist, target) ||
            _whitelist.IsWhitelistPass(comp.Blacklist, target))
            return false;

        var attemptEv = new AttemptEntityStickEvent(target, user);
        RaiseLocalEvent(uid, ref attemptEv);
        if (attemptEv.Cancelled)
            return false;

        // skip doafter and popup if it's instant
        if (comp.StickDelay <= TimeSpan.Zero)
        {
            StickToEntity(ent, target, user);
            return true;
        }

        // show message to user
        if (comp.StickPopupStart != null)
        {
            var msg = Loc.GetString(comp.StickPopupStart);
            _popup.PopupClient(msg, user, user);
        }

        // start sticking object to target
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, comp.StickDelay, new StickyDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnMove = true,
            NeedHand = true,
        });

        return true;
    }

    private void OnStickyDoAfter(Entity<StickyComponent> ent, ref StickyDoAfterEvent args)
    {
        // target is the sticky item when unsticking and the surface when sticking, it will never be null
        if (args.Handled || args.Cancelled || args.Args.Target is not {} target)
            return;

        var user = args.User;
        if (ent.Comp.StuckTo == null)
            StickToEntity(ent, target, user);
        else
            UnstickFromEntity(ent, user);

        args.Handled = true;
    }

    private void StartUnsticking(Entity<StickyComponent> ent, EntityUid user)
    {
        var (uid, comp) = ent;
        if (comp.StuckTo is not {} stuckTo)
            return;

        var attemptEv = new AttemptEntityUnstickEvent(stuckTo, user);
        RaiseLocalEvent(uid, ref attemptEv);
        if (attemptEv.Cancelled)
            return;

        // skip doafter and popup if it's instant
        if (comp.UnstickDelay <= TimeSpan.Zero)
        {
            UnstickFromEntity(ent, user);
            return;
        }

        // show message to user
        if (comp.UnstickPopupStart != null)
        {
            var msg = Loc.GetString(comp.UnstickPopupStart);
            _popup.PopupClient(msg, user, user);
        }

        // start unsticking object
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, comp.UnstickDelay, new StickyDoAfterEvent(), uid, target: uid)
        {
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    public void StickToEntity(Entity<StickyComponent> ent, EntityUid target, EntityUid user)
    {
        var (uid, comp) = ent;
        var attemptEv = new AttemptEntityStickEvent(target, user);
        RaiseLocalEvent(uid, ref attemptEv);
        if (attemptEv.Cancelled)
            return;

        // add container to entity and insert sticker into it
        var container = _container.EnsureContainer<Container>(target, StickerSlotId);
        container.ShowContents = true;
        if (!_container.Insert(uid, container))
            return;

        // show message to user
        if (comp.StickPopupSuccess != null)
        {
            var msg = Loc.GetString(comp.StickPopupSuccess);
            _popup.PopupClient(msg, user, user);
        }

        // send information to appearance that entity is stuck
        _appearance.SetData(uid, StickyVisuals.IsStuck, true);

        comp.StuckTo = target;
        Dirty(uid, comp);

        var ev = new EntityStuckEvent(target, user);
        RaiseLocalEvent(uid, ref ev);
    }

    public void UnstickFromEntity(Entity<StickyComponent> ent, EntityUid user)
    {
        var (uid, comp) = ent;
        if (comp.StuckTo is not {} stuckTo)
            return;

        var attemptEv = new AttemptEntityUnstickEvent(stuckTo, user);
        RaiseLocalEvent(uid, ref attemptEv);
        if (attemptEv.Cancelled)
            return;

        // try to remove sticky item from target container
        if (!_container.TryGetContainer(stuckTo, StickerSlotId, out var container) || !_container.Remove(uid, container))
            return;

        // delete container if it's now empty
        if (container.ContainedEntities.Count == 0)
            _container.ShutdownContainer(container);

        // try place dropped entity into user hands
        _hands.PickupOrDrop(user, uid);

        // send information to appearance that entity isn't stuck
        _appearance.SetData(uid, StickyVisuals.IsStuck, false);

        // show message to user
        if (comp.UnstickPopupSuccess != null)
        {
            var msg = Loc.GetString(comp.UnstickPopupSuccess);
            _popup.PopupClient(msg, user, user);
        }

        comp.StuckTo = null;
        Dirty(uid, comp);

        var ev = new EntityUnstuckEvent(stuckTo, user);
        RaiseLocalEvent(uid, ref ev);
    }
}
