using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Lock;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Resist;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;

namespace Content.Server.Resist;

public sealed class ResistLockerSystem : EntitySystem
{
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly LockSystem _lockSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly WeldableSystem _weldable = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ResistLockerComponent, ContainerRelayMovementEntityEvent>(OnRelayMovement);
        SubscribeLocalEvent<ResistLockerComponent, ResistLockerDoAfterEvent>(OnDoAfter);
    }

    private void OnRelayMovement(EntityUid uid, ResistLockerComponent component, ref ContainerRelayMovementEntityEvent args)
    {
        if (component.IsResisting)
            return;

        if (!TryComp(uid, out EntityStorageComponent? storageComponent))
            return;

        if (TryComp<LockComponent>(uid, out var lockComponent) && lockComponent.Locked || _weldable.IsWelded(uid))
        {
            AttemptResist(args.Entity, uid, storageComponent, component);
        }
    }

    private void AttemptResist(EntityUid user, EntityUid target, EntityStorageComponent? storageComponent = null, ResistLockerComponent? resistLockerComponent = null)
    {
        if (!Resolve(target, ref storageComponent, ref resistLockerComponent))
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, resistLockerComponent.ResistTime, new ResistLockerDoAfterEvent(), target, target: target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false //No hands 'cause we be kickin'
        };

        resistLockerComponent.IsResisting = true;
        _popupSystem.PopupEntity(Loc.GetString("resist-locker-component-start-resisting"), user, user, PopupType.Large);
        _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnDoAfter(EntityUid uid, ResistLockerComponent component, DoAfterEvent args)
    {
        if (args.Cancelled)
        {
            component.IsResisting = false;
            _popupSystem.PopupEntity(Loc.GetString("resist-locker-component-resist-interrupted"), args.Args.User, args.Args.User, PopupType.Medium);
            return;
        }

        if (args.Handled || args.Args.Target == null)
            return;

        component.IsResisting = false;

        if (HasComp<EntityStorageComponent>(uid))
        {
            WeldableComponent? weldable = null;
            if (_weldable.IsWelded(uid, weldable))
                _weldable.SetWeldedState(uid, false, weldable);

            if (TryComp<LockComponent>(args.Args.Target.Value, out var lockComponent))
                _lockSystem.Unlock(uid, args.Args.User, lockComponent);

            _entityStorage.TryOpenStorage(args.Args.User, uid);
        }

        args.Handled = true;
    }
}
