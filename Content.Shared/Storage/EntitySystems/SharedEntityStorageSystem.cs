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
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Content.Shared.Wall;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Storage.EntitySystems;

public abstract class SharedEntityStorageSystem : EntitySystem
{
    [Dependency] private   readonly IGameTiming _timing = default!;
    [Dependency] private   readonly INetManager _net = default!;
    [Dependency] private   readonly EntityLookupSystem _lookup = default!;
    [Dependency] private   readonly PlaceableSurfaceSystem _placeableSurface = default!;
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

        if (_timing.CurTime < component.NextInternalOpenAttempt)
            return;

        component.NextInternalOpenAttempt = _timing.CurTime + SharedEntityStorageComponent.InternalOpenAttemptDelay;
        Dirty(uid, component);

        if (component.OpenOnMove)
        {
            TryOpenStorage(args.Entity, uid);
        }
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

        var targetCoordinates = new EntityCoordinates(uid, component.EnteringOffset);

        var entities = _lookup.GetEntitiesInRange(targetCoordinates, component.EnteringRange, LookupFlags.Approximate | LookupFlags.Dynamic | LookupFlags.Sundries);

        var ev = new StorageBeforeCloseEvent(entities, new());
        RaiseLocalEvent(uid, ref ev);
        var count = 0;
        foreach (var entity in ev.Contents)
        {
            if (!ev.BypassChecks.Contains(entity))
            {
                if (!CanInsert(entity, uid, component))
                    continue;
            }

            if (!AddToContents(entity, uid, component))
                continue;

            count++;
            if (count >= component.Capacity)
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

        RemComp<InsideEntityStorageComponent>(toRemove);
        _container.Remove(toRemove, component.Contents);
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

        return CanFit(toInsert, container, component);
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

        if (_container.IsEntityInContainer(target))
        {
            if (_container.TryGetOuterContainer(target,Transform(target) ,out var container) &&
                !HasComp<HandsComponent>(container.Owner))
            {
                Popup.PopupClient(Loc.GetString("entity-storage-component-already-contains-user-message"), user, user);

                return false;
            }
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

        var aabb = _lookup.GetAABBNoContainer(toAdd, Vector2.Zero, 0);
        if (component.MaxSize < aabb.Size.X || component.MaxSize < aabb.Size.Y)
            return false;

        return Insert(toAdd, container, component);
    }

    private bool CanFit(EntityUid toInsert, EntityUid container, SharedEntityStorageComponent? component = null)
    {
        if (!Resolve(container, ref component))
            return false;

        // conditions are complicated because of pizzabox-related issues, so follow this guide
        // 0. Accomplish your goals at all costs.
        // 1. AddToContents can block anything
        // 2. maximum item count can block anything
        // 3. ghosts can NEVER be eaten
        // 4. items can always be eaten unless a previous law prevents it
        // 5. if this is NOT AN ITEM, then mobs can always be eaten unless a previous
        // law prevents it
        // 6. if this is an item, then mobs must only be eaten if some other component prevents
        // pick-up interactions while a mob is inside (e.g. foldable)
        var attemptEvent = new InsertIntoEntityStorageAttemptEvent();
        RaiseLocalEvent(toInsert, ref attemptEvent);
        if (attemptEvent.Cancelled)
            return false;

        var targetIsMob = HasComp<BodyComponent>(toInsert);
        var storageIsItem = HasComp<ItemComponent>(container);
        var allowedToEat = component.Whitelist == null ? HasComp<ItemComponent>(toInsert) : _whitelistSystem.IsValid(component.Whitelist, toInsert);

        // BEFORE REPLACING THIS WITH, I.E. A PROPERTY:
        // Make absolutely 100% sure you have worked out how to stop people ending up in backpacks.
        // Seriously, it is insanely hacky and weird to get someone out of a backpack once they end up in there.
        // And to be clear, they should NOT be in there.
        // For the record, what you need to do is empty the backpack onto a PlacableSurface (table, rack)
        if (targetIsMob)
        {
            if (!storageIsItem)
                allowedToEat = true;
            else
            {
                var storeEv = new StoreMobInItemContainerAttemptEvent();
                RaiseLocalEvent(container, ref storeEv);
                allowedToEat = storeEv is { Handled: true, Cancelled: false };

                if (component.ItemCanStoreMobs)
                    allowedToEat = true;
            }
        }

        return allowedToEat;
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

        if (TryComp<PlaceableSurfaceComponent>(uid, out var surface))
            _placeableSurface.SetPlaceable(uid, component.Open, surface);

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
