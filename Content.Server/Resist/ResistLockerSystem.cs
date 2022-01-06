using Content.Shared.Movement;
using Robust.Shared.GameObjects;
using Content.Server.Storage.Components;
using Content.Server.DoAfter;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Containers;
using Content.Server.Popups;
using Robust.Shared.Localization;

namespace Content.Server.Resist;

public class ResistLockerSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ResistLockerComponent, RelayMovementEntityEvent>(OnRelayMovement);
        SubscribeLocalEvent<ResistLockerComponent, ResistDoAfterComplete>(OnDoAfterComplete);
        SubscribeLocalEvent<ResistLockerComponent, ResistDoAfterCancelled>(OnDoAfterCancelled);
        SubscribeLocalEvent<ResistLockerComponent, EntRemovedFromContainerMessage>(OnRemovedFromContainer);
    }

    private void OnRelayMovement(EntityUid uid, ResistLockerComponent component, RelayMovementEntityEvent args)
    {
        TryComp(uid, out EntityStorageComponent? storageComponent);
        if (!Resolve(uid, ref storageComponent))
            return;

        if (!component.IsResisting)
        {
            if (TryComp<LockComponent>(uid, out var lockComponent) && lockComponent.Locked || storageComponent.IsWeldedShut)
            {
                AttemptResist(args.Entity, uid, storageComponent, component);
            }
        }
    }

    private void AttemptResist(EntityUid user, EntityUid target, EntityStorageComponent? storageComponent, ResistLockerComponent? resistLockerComponent)
    {
        if (!Resolve(target, ref storageComponent, ref resistLockerComponent))
            return;

        resistLockerComponent.CancelToken = new();
        var doAfterEventArgs = new DoAfterEventArgs(user, resistLockerComponent.ResistTime, resistLockerComponent.CancelToken.Token, target)
        {
            BreakOnTargetMove = false,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnStun = true,
            NeedHand = false, //No hands 'cause we be kickin'
            TargetFinishedEvent = new ResistDoAfterComplete(user, target),
            TargetCancelledEvent = new ResistDoAfterCancelled(user)
        };

        resistLockerComponent.IsResisting = true;
        _popupSystem.PopupEntity(Loc.GetString("resist-locker-component-start-resisting"), user, Filter.Entities(user));
        _doAfterSystem.DoAfter(doAfterEventArgs);
    }

    private void OnDoAfterComplete(EntityUid uid, ResistLockerComponent component, ResistDoAfterComplete ev)
    {
        component.IsResisting = false;

        if (TryComp<EntityStorageComponent>(uid, out var storageComponent))
        {
            if (storageComponent.IsWeldedShut)
                storageComponent.IsWeldedShut = false;

            if (TryComp<LockComponent>(ev.Target, out var lockComponent))
            {
                lockComponent.Locked = false;
            }

            component.CancelToken = null;
            storageComponent.TryOpenStorage(ev.User);
        }
    }

    private void OnDoAfterCancelled(EntityUid uid, ResistLockerComponent component, ResistDoAfterCancelled ev)
    {
        component.IsResisting = false;
        component.CancelToken = null;
        _popupSystem.PopupEntity(Loc.GetString("resist-locker-component-resist-interrupted"), ev.User, Filter.Entities(ev.User));
    }

    private void OnRemovedFromContainer(EntityUid uid, ResistLockerComponent component, EntRemovedFromContainerMessage message)
    {
        component.CancelToken?.Cancel();
        component.CancelToken = null;
    }
    private class ResistDoAfterComplete : EntityEventArgs
    {
        public readonly EntityUid User;
        public readonly EntityUid Target;
        public ResistDoAfterComplete(EntityUid userUid, EntityUid target)
        {
            User = userUid;
            Target = target;
        }
    }

    private class ResistDoAfterCancelled : EntityEventArgs
    {
        public readonly EntityUid User;

        public ResistDoAfterCancelled(EntityUid userUid)
        {
            User = userUid;
        }
    }
}
