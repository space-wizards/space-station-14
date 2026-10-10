using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Lock;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Resist.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Tools.Systems;

namespace Content.Shared.Resist.EntitySystems;

/// <summary>
/// Handles allowing entities with <see cref="ResistLockerComponent"/> to break out of locked or welded containers by moving.
/// </summary>
public sealed partial class ResistLockerSystem : EntitySystem
{
    [Dependency] private SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private LockSystem _lock = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private WeldableSystem _weldable = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;

    [SubscribeLocalEvent]
    private void OnRelayMovement(Entity<ResistLockerComponent> ent, ref ContainerRelayMovementEntityEvent args)
    {
        if (ent.Comp.IsResisting)
            return;

        if (CanResist(ent, args.Entity))
            AttemptResist(ent.Owner, args.Entity);
    }

    [SubscribeLocalEvent]
    private void OnDoAfterAttempt(Entity<ResistLockerComponent> ent, ref DoAfterAttemptEvent<ResistLockerDoAfterEvent> args)
    {
        if (!CanResist(ent, args.DoAfter.Args.User))
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<ResistLockerComponent> ent, ref ResistLockerDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.IsResisting)
        {
            ent.Comp.IsResisting = false;
            DirtyField(ent, ent.Comp, nameof(ResistLockerComponent.IsResisting));
        }

        if (args.Cancelled)
        {
            _popup.PopupEntity(Loc.GetString("resist-locker-component-resist-interrupted"), args.User, args.User, PopupType.Medium);
            return;
        }

        if (TryComp<EntityStorageComponent>(ent, out var storageComp))
        {
            _weldable.SetWeldedState(ent, false);
            _lock.Unlock(ent, args.User);

            if (storageComp.OpenOnMove)
                _entityStorage.TryOpenStorage(args.User, ent);
        }

        args.Handled = true;
    }

    private void AttemptResist(Entity<ResistLockerComponent?, EntityStorageComponent?> ent, EntityUid user)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager,
            user,
            ent.Comp1.ResistTime,
            new ResistLockerDoAfterEvent(),
            ent.Owner,
            target: ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false, // No hands 'cause we be kickin'.
            AttemptFrequency = AttemptFrequency.EveryTick,
        };

        if (!_doAfter.TryStartDoAfter(doAfterEventArgs, out var doAfterId)
            || !_doAfter.IsRunning(doAfterId))
            return;

        ent.Comp1.IsResisting = true;
        DirtyField(ent, ent.Comp1, nameof(ResistLockerComponent.IsResisting));
        _popup.PopupEntity(Loc.GetString("resist-locker-component-start-resisting"), user, user, PopupType.Large);
    }

    private bool CanResist(Entity<ResistLockerComponent> ent, EntityUid user)
    {
        return TryComp<EntityStorageComponent>(ent, out var storageComp)
               && storageComp.OpenOnMove
               && storageComp.Contents.Contains(user)
               && _actionBlocker.CanMove(user)
               && (_lock.IsLocked(ent.Owner) || _weldable.IsWelded(ent.Owner));
    }
}
