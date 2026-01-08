using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Lock;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Resist.Components;
using Content.Shared.Resist;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;

namespace Content.Shared.Resist.EntitySystems;

/// <summary>
/// Handles allowing entities with <see cref="ResistLockerComponent"/> to break out of locked or welded containers by moving.
/// </summary>
public sealed class ResistLockerSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly WeldableSystem _weldable = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    /// <inheritdoc/>
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

        if (!HasComp<EntityStorageComponent>(ent.Owner))
            return;

        if (!_actionBlocker.CanMove(args.Entity))
            return;

        if (TryComp<LockComponent>(ent.Owner, out var lockComponent) && lockComponent.Locked || _weldable.IsWelded(ent.Owner))
            AttemptResist(ent, args.Entity);
    }

    private void AttemptResist(Entity<ResistLockerComponent> ent, EntityUid user)
    {
        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, ent.Comp.ResistTime, new ResistLockerDoAfterEvent(), ent.Owner, target: ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false, // No hands 'cause we be kickin'.
        };

        // Make sure the do after is able to start.
        if (!_doAfter.TryStartDoAfter(doAfterEventArgs))
            return;

        ent.Comp.IsResisting = true;
        Dirty(ent);
        _popup.PopupClient(Loc.GetString("resist-locker-component-start-resisting"), user, user, PopupType.Large);
    }

    private void OnDoAfter(Entity<ResistLockerComponent> ent, ref ResistLockerDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            ent.Comp.IsResisting = false;
            Dirty(ent);
            _popup.PopupClient(Loc.GetString("resist-locker-component-resist-interrupted"), args.Args.User, args.Args.User, PopupType.Medium);
            return;
        }

        if (args.Handled || args.Args.Target == null)
            return;

        ent.Comp.IsResisting = false;
        Dirty(ent);

        if (HasComp<EntityStorageComponent>(ent.Owner))
        {
            WeldableComponent? weldable = null;
            if (_weldable.IsWelded(ent.Owner, weldable))
                _weldable.SetWeldedState(ent.Owner, false, weldable);

            if (TryComp<LockComponent>(args.Args.Target.Value, out var lockComponent))
                _lock.Unlock(ent.Owner, args.Args.User, lockComponent);

            _entityStorage.TryOpenStorage(args.Args.User, ent.Owner);
        }

        args.Handled = true;
    }
}
