using Content.Shared.DoAfter;
using Content.Shared.Lock;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Storage.EntitySystems;

namespace Content.Shared.Resist;

public sealed class ResistLockerSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly LockSystem _lockSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly WeldableSystem _weldable = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ResistLockerComponent, ContainerRelayMovementEntityEvent>(OnRelayMovement);
        SubscribeLocalEvent<ResistLockerComponent, ResistLockerDoAfterEvent>(OnDoAfter);
    }

    private void OnRelayMovement(Entity<ResistLockerComponent> ent, ref ContainerRelayMovementEntityEvent args)
    {
        if (ent.Comp.IsResisting)
            return;

        if (!TryComp(ent, out EntityStorageComponent? storageComponent))
            return;

        if (!_actionBlocker.CanMove(args.Entity))
            return;

        if (TryComp<LockComponent>(ent, out var lockComponent) && lockComponent.Locked || _weldable.IsWelded(ent))
        {
            AttemptResist(args.Entity, ent, storageComponent, ent.Comp);
            Dirty(ent);
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
            NeedHand = false, //No hands 'cause we be kickin'
        };

        // Make sure the do after is able to start
        if (!_doAfterSystem.TryStartDoAfter(doAfterEventArgs))
            return;

        resistLockerComponent.IsResisting = true;
        _popupSystem.PopupEntity(Loc.GetString("resist-locker-component-start-resisting"), user, user, PopupType.Large);
    }

    private void OnDoAfter(Entity<ResistLockerComponent> ent, ref ResistLockerDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            ent.Comp.IsResisting = false;
            _popupSystem.PopupClient(Loc.GetString("resist-locker-component-resist-interrupted"), args.Args.User, args.Args.User, PopupType.Medium);
            return;
        }

        if (args.Handled || args.Args.Target == null)
            return;

        ent.Comp.IsResisting = false;

        if (HasComp<EntityStorageComponent>(ent))
        {
            WeldableComponent? weldable = null;
            if (_weldable.IsWelded(ent, weldable))
                _weldable.SetWeldedState(ent, false, weldable);

            if (TryComp<LockComponent>(args.Args.Target.Value, out var lockComponent))
                _lockSystem.Unlock(ent, args.Args.User, lockComponent);

            _entityStorage.TryOpenStorage(args.Args.User, ent);
        }

        args.Handled = true;
    }
}
