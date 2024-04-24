using Content.Server.Popups;
using Content.Server.Sticky.Components;
using Content.Server.Sticky.Events;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Sticky;
using Content.Shared.Sticky.Components;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server.Sticky.Systems;

public sealed class StickySystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private const string StickerSlotId = "stickers_container";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StickyComponent, StickyDoAfterEvent>(OnStickFinished);
        SubscribeLocalEvent<StickyComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<StickyComponent, GetVerbsEvent<Verb>>(AddUnstickVerb);
    }

    private void OnAfterInteract(EntityUid uid, StickyComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        // try stick object to a clicked target entity
        args.Handled = StartSticking(uid, args.User, args.Target.Value, component);
    }

    private void AddUnstickVerb(EntityUid uid, StickyComponent component, GetVerbsEvent<Verb> args)
    {
        if (component.StuckTo == null || !component.CanUnstick || !args.CanInteract || args.Hands == null)
            return;

        // we can't use args.CanAccess, because it stuck in another container
        // we also need to ignore entity that it stuck to
        var inRange = _interactionSystem.InRangeUnobstructed(uid, args.User,
            predicate: entity => component.StuckTo == entity);
        if (!inRange)
            return;

        args.Verbs.Add(new Verb
        {
            DoContactInteraction = true,
            Text = Loc.GetString("comp-sticky-unstick-verb-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
            Act = () => StartUnsticking(uid, args.User, component)
        });
    }

    private bool StartSticking(EntityUid uid, EntityUid user, EntityUid target, StickyComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        // check whitelist and blacklist
        if (component.Whitelist != null && !component.Whitelist.IsValid(target))
            return false;
        if (component.Blacklist != null && component.Blacklist.IsValid(target))
            return false;

        var attemptEv = new AttemptEntityStickEvent(target, user);
        RaiseLocalEvent(uid, ref attemptEv);
        if (attemptEv.Cancelled)
            return false;

        // check if delay is not zero to start do after
        var delay = (float) component.StickDelay.TotalSeconds;
        if (delay > 0)
        {
            // show message to user
            if (component.StickPopupStart != null)
            {
                var msg = Loc.GetString(component.StickPopupStart);
                _popupSystem.PopupEntity(msg, user, user);
            }

            component.Stick = true;

            // start sticking object to target
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, user, delay, new StickyDoAfterEvent(), uid, target: target, used: uid)
            {
                BreakOnMove = true,
                NeedHand = true
            });
        }
        else
        {
            // if delay is zero - stick entity immediately
            StickToEntity(uid, target, user, component);
        }

        return true;
    }

    private void OnStickFinished(EntityUid uid, StickyComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        if (component.Stick)
            StickToEntity(uid, args.Args.Target.Value, args.Args.User, component);
        else
            UnstickFromEntity(uid, args.Args.User, component);

        args.Handled = true;
    }

    private void StartUnsticking(EntityUid uid, EntityUid user, StickyComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.StuckTo is not { } stuckTo)
            return;

        var attemptEv = new AttemptEntityUnstickEvent(stuckTo, user);
        RaiseLocalEvent(uid, ref attemptEv);
        if (attemptEv.Cancelled)
            return;

        var delay = (float) component.UnstickDelay.TotalSeconds;
        if (delay > 0)
        {
            // show message to user
            if (component.UnstickPopupStart != null)
            {
                var msg = Loc.GetString(component.UnstickPopupStart);
                _popupSystem.PopupEntity(msg, user, user);
            }

            component.Stick = false;

            // start unsticking object
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, user, delay, new StickyDoAfterEvent(), uid, target: uid)
            {
                BreakOnMove = true,
                NeedHand = true
            });
        }
        else
        {
            // if delay is zero - unstick entity immediately
            UnstickFromEntity(uid, user, component);
        }
    }

    public void StickToEntity(EntityUid uid, EntityUid target, EntityUid user, StickyComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var attemptEv = new AttemptEntityStickEvent(target, user);
        RaiseLocalEvent(uid, ref attemptEv);
        if (attemptEv.Cancelled)
            return;

        // add container to entity and insert sticker into it
        var container = _containerSystem.EnsureContainer<Container>(target, StickerSlotId);
        container.ShowContents = true;
        if (!_containerSystem.Insert(uid, container))
            return;

        // show message to user
        if (component.StickPopupSuccess != null)
        {
            var msg = Loc.GetString(component.StickPopupSuccess);
            _popupSystem.PopupEntity(msg, user, user);
        }

        // send information to appearance that entity is stuck
        if (TryComp(uid, out AppearanceComponent? appearance))
        {
            _appearance.SetData(uid, StickyVisuals.IsStuck, true, appearance);
        }

        component.StuckTo = target;
        RaiseLocalEvent(uid, new EntityStuckEvent(target, user), true);
    }

    public void UnstickFromEntity(EntityUid uid, EntityUid user, StickyComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.StuckTo is not { } stuckTo)
            return;

        var attemptEv = new AttemptEntityUnstickEvent(stuckTo, user);
        RaiseLocalEvent(uid, ref attemptEv);
        if (attemptEv.Cancelled)
            return;

        // try to remove sticky item from target container
        if (!_containerSystem.TryGetContainer(stuckTo, StickerSlotId, out var container) || !_containerSystem.Remove(uid, container))
            return;
        // delete container if it's now empty
        if (container.ContainedEntities.Count == 0)
            _containerSystem.ShutdownContainer(container);

        // try place dropped entity into user hands
        _handsSystem.PickupOrDrop(user, uid);

        // send information to appearance that entity isn't stuck
        if (TryComp(uid, out AppearanceComponent? appearance))
        {
            _appearance.SetData(uid, StickyVisuals.IsStuck, false, appearance);
        }

        // show message to user
        if (component.UnstickPopupSuccess != null)
        {
            var msg = Loc.GetString(component.UnstickPopupSuccess);
            _popupSystem.PopupEntity(msg, user, user);
        }

        component.StuckTo = null;
        RaiseLocalEvent(uid, new EntityUnstuckEvent(stuckTo, user), true);
    }
}
