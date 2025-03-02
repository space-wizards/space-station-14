using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared.Body.Components;
using Content.Shared.Destructible;
using Content.Shared.Foldable;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Lock;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Content.Shared.Wall;
using Content.Shared.Whitelist;
using Content.Shared.ActionBlocker;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Storage.EntitySystems;

public abstract class SharedEntityStorageSystem : EntitySystem
{
    [Dependency] private   readonly IGameTiming _timing = default!;
    [Dependency] private   readonly INetManager _net = default!;
    [Dependency] private   readonly EntityLookupSystem _lookup = default!;
    [Dependency] private   readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private   readonly SharedAudioSystem _audio = default!;
    [Dependency] private   readonly SharedContainerSystem _container = default!;
    [Dependency] private   readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private   readonly SharedJointSystem _joints = default!;
    [Dependency] private   readonly SharedPhysicsSystem _physics = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] private   readonly WeldableSystem _weldable = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    public const string ContainerName = "entity_storage";

    protected void OnEntityUnpausedEvent(EntityUid uid, SharedEntityStorageComponent component, EntityUnpausedEvent args)
    {
        component.NextInternalOpenAttempt += args.PausedTime;
    }

    protected void OnGetState(EntityUid uid, SharedEntityStorageComponent component, ref ComponentGetState args)
    {
        args.State = new EntityStorageComponentState(component.Open,
            component.Capacity,
            component.IsCollidableWhenOpen,
            component.OpenOnMove,
            component.EnteringRange,
            component.NextInternalOpenAttempt);
    }

    protected void OnHandleState(EntityUid uid, SharedEntityStorageComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not EntityStorageComponentState state)
            return;
        component.Open = state.Open;
        component.Capacity = state.Capacity;
        component.IsCollidableWhenOpen = state.IsCollidableWhenOpen;
        component.OpenOnMove = state.OpenOnMove;
        component.EnteringRange = state.EnteringRange;
        component.NextInternalOpenAttempt = state.NextInternalOpenAttempt;
    }

    protected virtual void OnComponentInit(EntityUid uid, SharedEntityStorageComponent component, ComponentInit args)
    {
        component.Contents = _container.EnsureContainer<Container>(uid, ContainerName);
        component.Contents.ShowContents = component.ShowContents;
        component.Contents.OccludesLight = component.OccludesLight;
    }

    protected virtual void OnComponentStartup(EntityUid uid, SharedEntityStorageComponent component, ComponentStartup args)
    {
        _appearance.SetData(uid, StorageVisuals.Open, component.Open);
    }

    protected void OnInteract(EntityUid uid, SharedEntityStorageComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        args.Handled = true;
        ToggleOpen(args.User, uid, component);
    }

    public abstract bool ResolveStorage(EntityUid uid, [NotNullWhen(true)] ref SharedEntityStorageComponent? component);

    protected void OnLockToggleAttempt(EntityUid uid, SharedEntityStorageComponent target, ref LockToggleAttemptEvent args)
    {
        // Cannot (un)lock open lockers.
        if (target.Open)
            args.Cancelled = true;

        // Cannot (un)lock from the inside. Maybe a bad idea? Security jocks could trap nerds in lockers?
        if (target.Contents.Contains(args.User))
            args.Cancelled = true;
    }

    protected void OnDestruction(EntityUid uid, SharedEntityStorageComponent component, DestructionEventArgs args)
    {
        component.Open = true;
        Dirty(uid, component);
        if (!component.DeleteContentsOnDestruction)
        {
            EmptyContents(uid, component);
            return;
        }

        foreach (var ent in new List<EntityUid>(component.Contents.ContainedEntities))
        {
            Del(ent);
        }
    }

    protected void OnRelayMovement(EntityUid uid, SharedEntityStorageComponent component, ref ContainerRelayMovementEntityEvent args)
    {
        if (!HasComp<HandsComponent>(args.Entity))
            return;

        if (!_actionBlocker.CanMove(args.Entity))
            return;

        if (_timing.CurTime < component.NextInternalOpenAttempt)
            return;

        component.NextInternalOpenAttempt = _timing.CurTime + SharedEntityStorageComponent.InternalOpenAttemptDelay;
        Dirty(uid, component);

        if (component.OpenOnMove)
            TryOpenStorage(args.Entity, uid);
    }

    protected void OnFoldAttempt(EntityUid uid, SharedEntityStorageComponent component, ref FoldAttemptEvent args)
    {
        if (args.Cancelled)
            return;
        args.Cancelled = component.Open || component.Contents.ContainedEntities.Count != 0;
    }

    protected void AddToggleOpenVerb(EntityUid uid, SharedEntityStorageComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!CanOpen(args.User, args.Target, silent: true, component))
            return;

        InteractionVerb verb = new();
        if (component.Open)
        {
            verb.Text = Loc.GetString("verb-common-close");
            verb.Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/close.svg.192dpi.png"));
        }
        else
        {
            verb.Text = Loc.GetString("verb-common-open");
            verb.Icon = new SpriteSpecifier.Texture(
                new("/Textures/Interface/VerbIcons/open.svg.192dpi.png"));
        }
        verb.Act = () => ToggleOpen(args.User, args.Target, component);
        args.Verbs.Add(verb);
    }


    public void ToggleOpen(EntityUid user, EntityUid target, SharedEntityStorageComponent? component = null)
    {
        if (!ResolveStorage(target, ref component))
            return;

        if (component.Open)
        {
            TryCloseStorage(target);
        }
        else
        {
            TryOpenStorage(user, target);
        }
    }

    public void EmptyContents(EntityUid uid, SharedEntityStorageComponent? component = null)
    {
        if (!ResolveStorage(uid, ref component))
            return;

        var uidXform = Transform(uid);
        var containedArr = component.Contents.ContainedEntities.ToArray();
        foreach (var contained in containedArr)
        {
            Remove(contained, uid, component, uidXform);
        }
    }

    public void OpenStorage(EntityUid uid, SharedEntityStorageComponent? component = null)
    {
        if (!ResolveStorage(uid, ref component))
            return;

        if (component.Open)
            return;

        var beforeev = new StorageBeforeOpenEvent();
        RaiseLocalEvent(uid, ref beforeev);
        component.Open = true;
        Dirty(uid, component);
        EmptyContents(uid, component);
        ModifyComponents(uid, component);
        if (_net.IsClient && _timing.IsFirstTimePredicted)
            _audio.PlayPvs(component.OpenSound, uid);
        ReleaseGas(uid, component);
        var afterev = new StorageAfterOpenEvent();
        RaiseLocalEvent(uid, ref afterev);
    }

    public void CloseStorage(EntityUid uid, SharedEntityStorageComponent? component = null)
    {
        if (!ResolveStorage(uid, ref component))
            return;

        if (!component.Open)
            return;

        // Prevent the container from closing if it is queued for deletion. This is so that the container-emptying
        // behaviour of DestructionEventArgs is respected. This exists because malicious players were using
        // destructible boxes to delete entities by having two players simultaneously destroy and close the box in
        // the same tick.
        if (EntityManager.IsQueuedForDeletion(uid))
            return;

        component.Open = false;
        Dirty(uid, component);

        var entities = _lookup.GetEntitiesInRange(
            new EntityCoordinates(uid, component.EnteringOffset),
            component.EnteringRange,
            LookupFlags.Approximate | LookupFlags.Dynamic | LookupFlags.Sundries
        );

        // Don't insert the container into itself.
        entities.Remove(uid);

        var ev = new StorageBeforeCloseEvent(entities, []);
        RaiseLocalEvent(uid, ref ev);

        foreach (var entity in ev.Contents)
        {
            if (!ev.BypassChecks.Contains(entity) && !CanInsert(entity, uid, component))
                continue;

            if (!AddToContents(entity, uid, component))
                continue;

            if (component.Contents.ContainedEntities.Count >= component.Capacity)
                break;
        }

        TakeGas(uid, component);
        ModifyComponents(uid, component);
        if (_net.IsClient && _timing.IsFirstTimePredicted)
            _audio.PlayPvs(component.CloseSound, uid);

        var afterev = new StorageAfterCloseEvent();
        RaiseLocalEvent(uid, ref afterev);
    }

    public bool Insert(EntityUid toInsert, EntityUid container, SharedEntityStorageComponent? component = null)
    {
        if (!ResolveStorage(container, ref component))
            return false;

        if (component.Open)
        {
            TransformSystem.DropNextTo(toInsert, container);
            return true;
        }

        _joints.RecursiveClearJoints(toInsert);
        if (!_container.Insert(toInsert, component.Contents))
            return false;

        var inside = EnsureComp<InsideEntityStorageComponent>(toInsert);
        inside.Storage = container;
        return true;
    }

    public bool Remove(EntityUid toRemove, EntityUid container, SharedEntityStorageComponent? component = null, TransformComponent? xform = null)
    {
        if (!Resolve(container, ref xform, false))
            return false;

        if (!ResolveStorage(container, ref component))
            return false;

        _container.Remove(toRemove, component.Contents);

        if (_container.IsEntityInContainer(container))
        {
            if (_container.TryGetOuterContainer(container, Transform(container), out var outerContainer) &&
                !HasComp<HandsComponent>(outerContainer.Owner))
            {
                _container.Insert(toRemove, outerContainer);
                return true;
            }
        }

        RemComp<InsideEntityStorageComponent>(toRemove);

        var pos = TransformSystem.GetWorldPosition(xform) + TransformSystem.GetWorldRotation(xform).RotateVec(component.EnteringOffset);
        TransformSystem.SetWorldPosition(toRemove, pos);
        return true;
    }

    public bool CanInsert(EntityUid toInsert, EntityUid container, SharedEntityStorageComponent? component = null)
    {
        if (!ResolveStorage(container, ref component))
            return false;

        if (component.Open)
            return true;

        if (component.Contents.ContainedEntities.Count >= component.Capacity)
            return false;

        var aabb = _lookup.GetAABBNoContainer(toInsert, Vector2.Zero, 0);
        if (component.MaxSize < aabb.Size.X || component.MaxSize < aabb.Size.Y)
            return false;

        // Allow other systems to prevent inserting the item: e.g. the item is actually a ghost.
        var attemptEvent = new InsertIntoEntityStorageAttemptEvent(toInsert);
        RaiseLocalEvent(toInsert, ref attemptEvent);

        if (attemptEvent.Cancelled)
            return false;

        // Consult the whitelist. The whitelist ignores the default assumption about how entity storage works.
        if (component.Whitelist != null)
            return _whitelistSystem.IsValid(component.Whitelist, toInsert);

        // The inserted entity must be a mob or an item.
        return HasComp<BodyComponent>(toInsert) || HasComp<ItemComponent>(toInsert);
    }

    public bool TryOpenStorage(EntityUid user, EntityUid target, bool silent = false)
    {
        if (!CanOpen(user, target, silent))
            return false;

        OpenStorage(target);
        return true;
    }

    public bool TryCloseStorage(EntityUid target)
    {
        if (!CanClose(target))
        {
            return false;
        }

        CloseStorage(target);
        return true;
    }

    public bool IsOpen(EntityUid target, SharedEntityStorageComponent? component = null)
    {
        if (!ResolveStorage(target, ref component))
            return false;

        return component.Open;
    }

    public bool CanOpen(EntityUid user, EntityUid target, bool silent = false, SharedEntityStorageComponent? component = null)
    {
        if (!ResolveStorage(target, ref component))
            return false;

        if (!HasComp<HandsComponent>(user))
            return false;

        if (_weldable.IsWelded(target))
        {
            if (!silent && !component.Contents.Contains(user))
                Popup.PopupClient(Loc.GetString("entity-storage-component-welded-shut-message"), target, user);

            return false;
        }

        //Checks to see if the opening position, if offset, is inside of a wall.
        if (component.EnteringOffset != new Vector2(0, 0) && !HasComp<WallMountComponent>(target)) //if the entering position is offset
        {
            var newCoords = new EntityCoordinates(target, component.EnteringOffset);
            if (!_interaction.InRangeUnobstructed(target, newCoords, 0, collisionMask: component.EnteringOffsetCollisionFlags))
            {
                if (!silent && _net.IsServer)
                    Popup.PopupEntity(Loc.GetString("entity-storage-component-cannot-open-no-space"), target);
                return false;
            }
        }

        var ev = new StorageOpenAttemptEvent(user, silent);
        RaiseLocalEvent(target, ref ev, true);

        return !ev.Cancelled;
    }

    public bool CanClose(EntityUid target, bool silent = false)
    {
        var ev = new StorageCloseAttemptEvent();
        RaiseLocalEvent(target, ref ev, silent);

        return !ev.Cancelled;
    }

    public bool AddToContents(EntityUid toAdd, EntityUid container, SharedEntityStorageComponent? component = null)
    {
        if (!ResolveStorage(container, ref component))
            return false;

        if (toAdd == container)
            return false;

        return Insert(toAdd, container, component);
    }

    private void ModifyComponents(EntityUid uid, SharedEntityStorageComponent? component = null)
    {
        if (!ResolveStorage(uid, ref component))
            return;

        if (!component.IsCollidableWhenOpen && TryComp<FixturesComponent>(uid, out var fixtures) &&
            fixtures.Fixtures.Count > 0)
        {
            // currently only works for single-fixture entities. If they have more than one fixture, then
            // RemovedMasks needs to be tracked separately for each fixture, using a fixture Id Dictionary. Also the
            // fixture IDs probably cant be automatically generated without causing issues, unless there is some
            // guarantee that they will get deserialized with the same auto-generated ID when saving+loading the map.
            var fixture = fixtures.Fixtures.First();

            if (component.Open)
            {
                component.RemovedMasks = fixture.Value.CollisionLayer & component.MasksToRemove;
                _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value, fixture.Value.CollisionLayer & ~component.MasksToRemove,
                    manager: fixtures);
            }
            else
            {
                _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value, fixture.Value.CollisionLayer | component.RemovedMasks,
                    manager: fixtures);
                component.RemovedMasks = 0;
            }
        }

        _appearance.SetData(uid, StorageVisuals.Open, component.Open);
        _appearance.SetData(uid, StorageVisuals.HasContents, component.Contents.ContainedEntities.Count > 0);
    }

    protected virtual void TakeGas(EntityUid uid, SharedEntityStorageComponent component)
    {

    }

    public virtual void ReleaseGas(EntityUid uid, SharedEntityStorageComponent component)
    {

    }
}
