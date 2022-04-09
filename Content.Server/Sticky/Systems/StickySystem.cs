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

        // show message to user
        if (component.StickPopupStart != null)
        {
            var msg = Loc.GetString(component.StickPopupStart);
            _popupSystem.PopupEntity(msg, args.User, Filter.Entities(args.User));
        }

        // start sticking object to target
        var delay = (float) component.Delay.TotalSeconds;
        _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, delay, target: args.Target)
        {
            BroadcastFinishedEvent = new StickySuccessfulEvent(args.User, args.Target.Value, component),
            BreakOnStun = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            NeedHand = true
        });

        args.Handled = true;
    }

    private void OnStickSuccessful(StickySuccessfulEvent ev)
    {
        StickToEntity(ev.Component.Owner, ev.Target, ev.User, ev.Component);
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

        RaiseLocalEvent(uid, new EntityStuckEvent(user));
    }

    private sealed class StickySuccessfulEvent : EntityEventArgs
    {
        public readonly EntityUid User;
        public readonly EntityUid Target;
        public readonly StickyComponent Component;

        public StickySuccessfulEvent(EntityUid user, EntityUid target, StickyComponent component)
        {
            User = user;
            Target = target;
            Component = component;
        }
    }
}
