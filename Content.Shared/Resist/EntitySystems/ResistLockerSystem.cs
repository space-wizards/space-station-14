using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Lock;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Resist.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Tools.Components;
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

        if (!TryComp<EntityStorageComponent>(ent, out var storageComp) || !storageComp.OpenOnMove)
            return;

        if (!_actionBlocker.CanMove(args.Entity))
            return;

        if (TryComp<LockComponent>(ent, out var lockComp) && lockComp.Locked || _weldable.IsWelded(ent.Owner))
            AttemptResist(ent.Owner, args.Entity);
    }

    // TODO: Convert to DoAfterAttemptEvent
    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<ResistLockerComponent> ent, ref DoAfterEvent args)
    {
        if (args.Handled)
            return;

        ent.Comp.IsResisting = false;
        Dirty(ent);

        if (args.Target != ent.Owner)
            return;

        if (args.Cancelled)
        {
            _popup.PopupEntity(Loc.GetString("resist-locker-component-resist-interrupted"), args.User, args.User, PopupType.Medium);
            return;
        }

        if (TryComp<EntityStorageComponent>(ent, out var storageComp))
        {
            WeldableComponent? weldable = null;
            if (_weldable.IsWelded(ent, weldable))
                _weldable.SetWeldedState(ent, false, weldable);

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
        };

        // Make sure the do after is able to start.
        if (!_doAfter.TryStartDoAfter(doAfterEventArgs))
            return;

        ent.Comp1.IsResisting = true;
        Dirty(ent, ent.Comp1);
        _popup.PopupEntity(Loc.GetString("resist-locker-component-start-resisting"), user, user, PopupType.Large);
    }
}
