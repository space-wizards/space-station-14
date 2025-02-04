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
using Content.Shared.ActionBlocker;
using Content.Shared.Jittering;

namespace Content.Server.Resist;

public sealed class ResistLockerSystem : EntitySystem
{
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly LockSystem _lockSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly WeldableSystem _weldable = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedJitteringSystem _jitteringSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ResistLockerComponent, ContainerRelayMovementEntityEvent>(OnRelayMovement);
        SubscribeLocalEvent<ResistLockerComponent, ResistLockerDoAfterEvent>(OnResistLockerDoafter);
        SubscribeLocalEvent<ResistLockerComponent, DoAfterAttemptEvent<ResistLockerDoAfterEvent>>((uid, comp, ev) =>
        {
            ResistDoafterEarlyCancel((uid, comp), ev.Event, ev);
        });
    }

    private void OnRelayMovement(EntityUid uid, ResistLockerComponent component, ref ContainerRelayMovementEntityEvent args)
    {
        if (!TryComp(uid, out EntityStorageComponent? storageComponent))
            return;

        if (!_actionBlocker.CanMove(args.Entity))
            return;

        if (TryComp<LockComponent>(uid, out var lockComponent) && lockComponent.Locked || _weldable.IsWelded(uid))
            AttemptResist(args.Entity, uid, storageComponent, component);
    }

    private void AttemptResist(EntityUid user, EntityUid target, EntityStorageComponent? storageComponent = null, ResistLockerComponent? resistLockerComponent = null)
    {
        if (!Resolve(target, ref storageComponent, ref resistLockerComponent))
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, resistLockerComponent.ResistTime, new ResistLockerDoAfterEvent(), target, target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            CancelDuplicate = false,
            AttemptFrequency = AttemptFrequency.EveryTick,
        };

        if (_doAfterSystem.TryStartDoAfter(doAfterEventArgs))
            _popupSystem.PopupEntity(Loc.GetString("resist-locker-component-start-resisting"), user, user, PopupType.Large);
    }

    private void ResistDoafterEarlyCancel(Entity<ResistLockerComponent> entity,
        ResistLockerDoAfterEvent args,
        CancellableEntityEventArgs ev)
    {
        if (args.Target == null)
        {
            ev.Cancel();
            _popupSystem.PopupEntity(Loc.GetString("resist-locker-component-resist-interrupted"), args.Args.User, args.Args.User, PopupType.Medium);
            return;
        }

        if (TryComp<WeldableComponent>(entity, out var weldableComponent) &&
            _weldable.IsWelded(entity, weldableComponent))
            return;

        if (TryComp<LockComponent>(entity, out var lockComponent) &&
            _lockSystem.IsLocked((entity, lockComponent)))
            return;

        ev.Cancel();
        _popupSystem.PopupEntity(Loc.GetString("resist-locker-component-resist-interrupted"), args.Args.User, args.Args.User, PopupType.Medium);
        _entityStorage.TryOpenStorage(args.User, entity);
    }

    private void OnResistLockerDoafter(Entity<ResistLockerComponent> entity, ref ResistLockerDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (HasComp<EntityStorageComponent>(entity))
        {
            WeldableComponent? weldable = null;
            if (_weldable.IsWelded(entity, weldable))
                _weldable.SetWeldedState(entity, false, weldable);

            if (TryComp<LockComponent>(args.Args.Target.Value, out var lockComponent))
                _lockSystem.Unlock(entity, args.Args.User, lockComponent);

            _entityStorage.TryOpenStorage(args.Args.User, entity);
        }

        args.Handled = true;
    }
}
