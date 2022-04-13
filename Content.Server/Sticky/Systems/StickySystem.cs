using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.Sticky.Components;
using Content.Server.Sticky.Events;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.Sticky.Systems;

public sealed class StickySystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    private const string StickerSlotId = "stickers_container";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StickySuccessfulEvent>(OnStickSuccessful);
        SubscribeLocalEvent<StickyComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(EntityUid uid, StickyComponent component, AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null)
            return;

        // check if delay is not zero to start do after
        var delay = (float) component.StickDelay.TotalSeconds;
        if (delay > 0)
        {
            // show message to user
            if (component.StickPopupStart != null)
            {
                var msg = Loc.GetString(component.StickPopupStart);
                _popupSystem.PopupEntity(msg, args.User, Filter.Entities(args.User));
            }

            // start sticking object to target
            _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, delay, target: args.Target)
            {
                BroadcastFinishedEvent = new StickySuccessfulEvent(uid, args.User, args.Target.Value),
                BreakOnStun = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true
            });
        }
        else
        {
            // if delay is zero - stick entity immediately
            StickToEntity(uid, args.Target.Value, args.User, component);
        }

        args.Handled = true;
    }

    private void OnStickSuccessful(StickySuccessfulEvent ev)
    {
        // check if entity still has sticky component
        if (!TryComp(ev.Uid, out StickyComponent? component))
            return;

        StickToEntity(ev.Uid, ev.Target, ev.User, component);
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
            _popupSystem.PopupEntity(msg, user, Filter.Entities(user));
        }

        // change sprite draw depth to show entity as overlay
        if (TryComp(uid, out SpriteComponent? sprite))
        {
            sprite.DrawDepth = component.StuckDrawDepth;
        }

        RaiseLocalEvent(uid, new EntityStuckEvent(target, user));
    }

    private sealed class StickySuccessfulEvent : EntityEventArgs
    {
        public readonly EntityUid Uid;
        public readonly EntityUid User;
        public readonly EntityUid Target;

        public StickySuccessfulEvent(EntityUid uid, EntityUid user, EntityUid target)
        {
            Uid = uid;
            User = user;
            Target = target;
        }
    }
}
