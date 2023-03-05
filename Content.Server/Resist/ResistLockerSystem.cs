using System.Threading;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Lock;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Robust.Shared.Containers;

namespace Content.Server.Resist;

public sealed class ResistLockerSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly LockSystem _lockSystem = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ResistLockerComponent, ContainerRelayMovementEntityEvent>(OnRelayMovement);
        SubscribeLocalEvent<ResistLockerComponent, DoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<ResistLockerComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnRelayMovement(EntityUid uid, ResistLockerComponent component, ref ContainerRelayMovementEntityEvent args)
    {
        if (component.IsResisting)
            return;

        if (!TryComp(uid, out EntityStorageComponent? storageComponent))
            return;

        if (TryComp<LockComponent>(uid, out var lockComponent) && lockComponent.Locked || storageComponent.IsWeldedShut)
        {
            AttemptResist(args.Entity, uid, storageComponent, component);
        }
    }

    private void AttemptResist(EntityUid user, EntityUid target, EntityStorageComponent? storageComponent = null, ResistLockerComponent? resistLockerComponent = null)
    {
        if (!Resolve(target, ref storageComponent, ref resistLockerComponent))
            return;

        resistLockerComponent.CancelToken = new CancellationTokenSource();

        var doAfterEventArgs = new DoAfterEventArgs(user, resistLockerComponent.ResistTime, cancelToken:resistLockerComponent.CancelToken.Token, target:target)
        {
            BreakOnTargetMove = false,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnStun = true,
            NeedHand = false //No hands 'cause we be kickin'
        };

        resistLockerComponent.IsResisting = true;
        _popupSystem.PopupEntity(Loc.GetString("resist-locker-component-start-resisting"), user, user, PopupType.Large);
        _doAfterSystem.DoAfter(doAfterEventArgs);
    }

    private void OnRemoved(EntityUid uid, ResistLockerComponent component, EntRemovedFromContainerMessage args)
    {
        component.CancelToken?.Cancel();
        component.CancelToken = null;
    }

    private void OnDoAfter(EntityUid uid, ResistLockerComponent component, DoAfterEvent args)
    {
        if (args.Cancelled)
        {
            component.IsResisting = false;
            component.CancelToken = null;
            _popupSystem.PopupEntity(Loc.GetString("resist-locker-component-resist-interrupted"), args.Args.User, args.Args.User, PopupType.Medium);
            return;
        }

        if (args.Handled || args.Args.Target == null)
            return;

        component.IsResisting = false;

        if (TryComp<EntityStorageComponent>(uid, out var storageComponent))
        {
            if (storageComponent.IsWeldedShut)
                storageComponent.IsWeldedShut = false;

            if (TryComp<LockComponent>(args.Args.Target.Value, out var lockComponent))
                _lockSystem.Unlock(uid, args.Args.User, lockComponent);

            _entityStorage.TryOpenStorage(args.Args.User, uid);
        }

        component.CancelToken = null;
        args.Handled = true;
    }
}
