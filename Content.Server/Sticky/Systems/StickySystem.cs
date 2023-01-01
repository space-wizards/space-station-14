using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.Sticky.Components;
using Content.Server.Sticky.Events;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Sticky.Components;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.Sticky.Systems;

public sealed class StickySystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

    private const string StickerSlotId = "stickers_container";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StickSuccessfulEvent>(OnStickSuccessful);
        SubscribeLocalEvent<UnstickSuccessfulEvent>(OnUnstickSuccessful);
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
            IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png",
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

            // start sticking object to target
            _doAfterSystem.DoAfter(new DoAfterEventArgs(user, delay, target: target)
            {
                BroadcastFinishedEvent = new StickSuccessfulEvent(uid, user, target),
                BreakOnStun = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
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

    private void OnStickSuccessful(StickSuccessfulEvent ev)
    {
        // check if entity still has sticky component
        if (!TryComp(ev.Uid, out StickyComponent? component))
            return;

        StickToEntity(ev.Uid, ev.Target, ev.User, component);
    }

    private void StartUnsticking(EntityUid uid, EntityUid user, StickyComponent? component = null)
    {
        if (!Resolve(uid, ref component))
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

            // start unsticking object
            _doAfterSystem.DoAfter(new DoAfterEventArgs(user, delay, target: uid)
            {
                BroadcastFinishedEvent = new UnstickSuccessfulEvent(uid, user),
                BreakOnStun = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true
            });
        }
        else
        {
            // if delay is zero - unstick entity immediately
            UnstickFromEntity(uid, user, component);
        }
    }

    private void OnUnstickSuccessful(UnstickSuccessfulEvent ev)
    {
        // check if entity still has sticky component
        if (!TryComp(ev.Uid, out StickyComponent? component))
            return;

        UnstickFromEntity(ev.Uid, ev.User, component);
    }

    public void StickToEntity(EntityUid uid, EntityUid target, EntityUid user, StickyComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        // add container to entity and insert sticker into it
        var container = _containerSystem.EnsureContainer<Container>(target, StickerSlotId);
        container.ShowContents = true;
        if (!container.Insert(uid))
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
            appearance.SetData(StickyVisuals.IsStuck, true);
        }

        component.StuckTo = target;
        RaiseLocalEvent(uid, new EntityStuckEvent(target, user), true);
    }

    public void UnstickFromEntity(EntityUid uid, EntityUid user, StickyComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        if (component.StuckTo == null)
            return;

        // try to remove sticky item from target container
        var target = component.StuckTo.Value;
        if (!_containerSystem.TryGetContainer(target, StickerSlotId, out var container) || !container.Remove(uid))
            return;
        // delete container if it's now empty
        if (container.ContainedEntities.Count == 0)
            container.Shutdown();

        // try place dropped entity into user hands
        _handsSystem.PickupOrDrop(user, uid);

        // send information to appearance that entity isn't stuck
        if (TryComp(uid, out AppearanceComponent? appearance))
        {
            appearance.SetData(StickyVisuals.IsStuck, false);
        }

        // show message to user
        if (component.UnstickPopupSuccess != null)
        {
            var msg = Loc.GetString(component.UnstickPopupSuccess);
            _popupSystem.PopupEntity(msg, user, user);
        }

        component.StuckTo = null;
        RaiseLocalEvent(uid, new EntityUnstuckEvent(target, user), true);
    }

    private sealed class StickSuccessfulEvent : EntityEventArgs
    {
        public readonly EntityUid Uid;
        public readonly EntityUid User;
        public readonly EntityUid Target;

        public StickSuccessfulEvent(EntityUid uid, EntityUid user, EntityUid target)
        {
            Uid = uid;
            User = user;
            Target = target;
        }
    }

    private sealed class UnstickSuccessfulEvent : EntityEventArgs
    {
        public readonly EntityUid Uid;
        public readonly EntityUid User;

        public UnstickSuccessfulEvent(EntityUid uid, EntityUid user)
        {
            Uid = uid;
            User = user;
        }
    }
}
