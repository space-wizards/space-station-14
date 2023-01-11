using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Server.Tools.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Destructible;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Placeable;
using Content.Shared.Storage;
using Content.Shared.Wall;
using Content.Shared.Whitelist;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Server.Storage.EntitySystems;

public sealed class EntityStorageSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ConstructionSystem _construction = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly PlaceableSurfaceSystem _placeableSurface = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly IMapManager _map = default!;

    public const string ContainerName = "entity_storage";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityStorageComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<EntityStorageComponent, ActivateInWorldEvent>(OnInteract);
        SubscribeLocalEvent<EntityStorageComponent, WeldableAttemptEvent>(OnWeldableAttempt);
        SubscribeLocalEvent<EntityStorageComponent, WeldableChangedEvent>(OnWelded);
        SubscribeLocalEvent<EntityStorageComponent, LockToggleAttemptEvent>(OnLockToggleAttempt);
        SubscribeLocalEvent<EntityStorageComponent, DestructionEventArgs>(OnDestruction);

        SubscribeLocalEvent<InsideEntityStorageComponent, EntGotRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<InsideEntityStorageComponent, InhaleLocationEvent>(OnInsideInhale);
        SubscribeLocalEvent<InsideEntityStorageComponent, ExhaleLocationEvent>(OnInsideExhale);
        SubscribeLocalEvent<InsideEntityStorageComponent, AtmosExposedGetAirEvent>(OnInsideExposed);

    }

    private void OnInit(EntityUid uid, EntityStorageComponent component, ComponentInit args)
    {
        component.Contents = _container.EnsureContainer<Container>(uid, ContainerName);
        component.Contents.ShowContents = component.ShowContents;
        component.Contents.OccludesLight = component.OccludesLight;

        if (TryComp<ConstructionComponent>(uid, out var construction))
            _construction.AddContainer(uid, ContainerName, construction);

        if (TryComp<PlaceableSurfaceComponent>(uid, out var placeable))
            _placeableSurface.SetPlaceable(uid, component.Open, placeable);

        if (!component.Open)
        {
            // If we're closed on spawn, we need to pull some air into our environment from where we spawned,
            // so that we have -something-. For example, if you bought an animal crate or something.
            TakeGas(uid, component);
        }
    }

    private void OnInteract(EntityUid uid, EntityStorageComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ToggleOpen(args.User, uid, component);
    }

    private void OnWeldableAttempt(EntityUid uid, EntityStorageComponent component, WeldableAttemptEvent args)
    {
        if (component.Open)
        {
            args.Cancel();
            return;
        }

        if (component.Contents.Contains(args.User))
        {
            var msg = Loc.GetString("entity-storage-component-already-contains-user-message");
            _popupSystem.PopupEntity(msg, args.User, args.User);
            args.Cancel();
        }
    }

    private void OnWelded(EntityUid uid, EntityStorageComponent component, WeldableChangedEvent args)
    {
        component.IsWeldedShut = args.IsWelded;
    }

    private void OnLockToggleAttempt(EntityUid uid, EntityStorageComponent target, ref LockToggleAttemptEvent args)
    {
        // Cannot (un)lock open lockers.
        if (target.Open)
            args.Cancelled = true;

        // Cannot (un)lock from the inside. Maybe a bad idea? Security jocks could trap nerds in lockers?
        if (target.Contents.Contains(args.User))
            args.Cancelled = true;
    }

    private void OnDestruction(EntityUid uid, EntityStorageComponent component, DestructionEventArgs args)
    {
        component.Open = true;
        if (!component.DeleteContentsOnDestruction)
        {
            EmptyContents(uid, component);
            return;
        }

        foreach (var ent in new List<EntityUid>(component.Contents.ContainedEntities))
        {
            EntityManager.DeleteEntity(ent);
        }
    }

    public void ToggleOpen(EntityUid user, EntityUid target, EntityStorageComponent? component = null)
    {
        if (!Resolve(target, ref component))
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

    public void EmptyContents(EntityUid uid, EntityStorageComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var uidXform = Transform(uid);
        var containedArr = component.Contents.ContainedEntities.ToArray();
        foreach (var contained in containedArr)
        {
            Remove(contained, uid, component, uidXform);
        }
    }

    public void OpenStorage(EntityUid uid, EntityStorageComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        RaiseLocalEvent(uid, new StorageBeforeOpenEvent());
        component.Open = true;
        EmptyContents(uid, component);
        ModifyComponents(uid, component);
        _audio.PlayPvs(component.OpenSound, component.Owner);
        ReleaseGas(uid, component);
        RaiseLocalEvent(uid, new StorageAfterOpenEvent());
    }

    public void CloseStorage(EntityUid uid, EntityStorageComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        component.Open = false;

        var targetCoordinates = new EntityCoordinates(uid, component.EnteringOffset);

        var entities = _lookup.GetEntitiesInRange(targetCoordinates, component.EnteringRange, LookupFlags.Approximate | LookupFlags.Dynamic | LookupFlags.Sundries);

        var ev = new StorageBeforeCloseEvent(entities);
        RaiseLocalEvent(uid, ev);
        var count = 0;
        foreach (var entity in ev.Contents)
        {
            if (!ev.BypassChecks.Contains(entity))
            {
                if (!CanFit(entity, uid, component.Whitelist))
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
        _audio.PlayPvs(component.CloseSound, component.Owner);
        component.LastInternalOpenAttempt = default;
        RaiseLocalEvent(uid, new StorageAfterCloseEvent());
    }

    public bool Insert(EntityUid toInsert, EntityUid container, EntityStorageComponent? component = null)
    {
        if (!Resolve(container, ref component))
            return false;

        if (component.Open)
        {
            Transform(toInsert).WorldPosition = Transform(container).WorldPosition;
            return true;
        }

        var inside = EnsureComp<InsideEntityStorageComponent>(toInsert);
        inside.Storage = container;
        return component.Contents.Insert(toInsert, EntityManager);
    }

    public bool Remove(EntityUid toRemove, EntityUid container, EntityStorageComponent? component = null, TransformComponent? xform = null)
    {
        if (!Resolve(container, ref component, ref xform, false))
            return false;

        RemComp<InsideEntityStorageComponent>(toRemove);
        component.Contents.Remove(toRemove, EntityManager);
        Transform(toRemove).WorldPosition = xform.WorldPosition + xform.WorldRotation.RotateVec(component.EnteringOffset);
        return true;
    }

    public bool CanInsert(EntityUid container, EntityStorageComponent? component = null)
    {
        if (!Resolve(container, ref component))
            return false;

        if (component.Open)
            return true;

        if (component.Contents.ContainedEntities.Count >= component.Capacity)
            return false;

        return true;
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

    public bool CanOpen(EntityUid user, EntityUid target, bool silent = false, EntityStorageComponent? component = null)
    {
        if (!Resolve(target, ref component))
            return false;

        if (!HasComp<SharedHandsComponent>(user))
            return false;

        if (component.IsWeldedShut)
        {
            if (!silent && !component.Contents.Contains(user))
                _popupSystem.PopupEntity(Loc.GetString("entity-storage-component-welded-shut-message"), target);

            return false;
        }

        //Checks to see if the opening position, if offset, is inside of a wall.
        if (component.EnteringOffset != (0, 0) && !HasComp<WallMountComponent>(target)) //if the entering position is offset
        {
            var newCoords = new EntityCoordinates(target, component.EnteringOffset);
            if (!_interactionSystem.InRangeUnobstructed(target, newCoords, 0, collisionMask: component.EnteringOffsetCollisionFlags))
            {
                if (!silent)
                    _popupSystem.PopupEntity(Loc.GetString("entity-storage-component-cannot-open-no-space"), target);
                return false;
            }
        }

        var ev = new StorageOpenAttemptEvent(silent);
        RaiseLocalEvent(target, ev, true);

        return !ev.Cancelled;
    }

    public bool CanClose(EntityUid target, bool silent = false)
    {
        var ev = new StorageCloseAttemptEvent();
        RaiseLocalEvent(target, ev, silent);

        return !ev.Cancelled;
    }

    public bool AddToContents(EntityUid toAdd, EntityUid container, EntityStorageComponent? component = null)
    {
        if (!Resolve(container, ref component))
            return false;

        if (toAdd == container)
            return false;

        if (TryComp<PhysicsComponent>(toAdd, out var phys))
        {
            if (component.MaxSize < phys.GetWorldAABB().Size.X || component.MaxSize < phys.GetWorldAABB().Size.Y)
                return false;
        }

        return Insert(toAdd, container, component);
    }

    public bool CanFit(EntityUid toInsert, EntityUid container, EntityWhitelist? whitelist)
    {
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
        RaiseLocalEvent(toInsert, attemptEvent);
        if (attemptEvent.Cancelled)
            return false;

        var targetIsMob = HasComp<BodyComponent>(toInsert);
        var storageIsItem = HasComp<ItemComponent>(container);
        var allowedToEat = whitelist?.IsValid(toInsert) ?? HasComp<ItemComponent>(toInsert);

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
                RaiseLocalEvent(container, storeEv);
                allowedToEat = storeEv.Handled && !storeEv.Cancelled;
            }
        }

        return allowedToEat;
    }

    public void ModifyComponents(EntityUid uid, EntityStorageComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!component.IsCollidableWhenOpen && TryComp<FixturesComponent>(uid, out var fixtures) && fixtures.Fixtures.Count > 0)
        {
            // currently only works for single-fixture entities. If they have more than one fixture, then
            // RemovedMasks needs to be tracked separately for each fixture, using a fixture Id Dictionary. Also the
            // fixture IDs probably cant be automatically generated without causing issues, unless there is some
            // guarantee that they will get deserialized with the same auto-generated ID when saving+loading the map.
            var fixture = fixtures.Fixtures.Values.First();

            if (component.Open)
            {
                component.RemovedMasks = fixture.CollisionLayer & component.MasksToRemove;
                fixture.CollisionLayer &= ~component.MasksToRemove;
            }
            else
            {
                fixture.CollisionLayer |= component.RemovedMasks;
                component.RemovedMasks = 0;
            }
        }

        if (TryComp<PlaceableSurfaceComponent>(uid, out var surface))
            _placeableSurface.SetPlaceable(uid, component.Open, surface);

        _appearance.SetData(uid, StorageVisuals.Open, component.Open);
        _appearance.SetData(uid, StorageVisuals.HasContents, component.Contents.ContainedEntities.Count > 0);
    }

    private void TakeGas(EntityUid uid, EntityStorageComponent component)
    {
        if (!component.Airtight)
            return;

        var tile = GetOffsetTileRef(uid, component);

        if (tile != null && _atmos.GetTileMixture(tile.Value.GridUid, null, tile.Value.GridIndices, true) is {} environment)
        {
            _atmos.Merge(component.Air, environment.RemoveVolume(EntityStorageComponent.GasMixVolume));
        }
    }

    public void ReleaseGas(EntityUid uid, EntityStorageComponent component)
    {
        if (!component.Airtight)
            return;

        var tile = GetOffsetTileRef(uid, component);

        if (tile != null && _atmos.GetTileMixture(tile.Value.GridUid, null, tile.Value.GridIndices, true) is {} environment)
        {
            _atmos.Merge(environment, component.Air);
            component.Air.Clear();
        }
    }

    private TileRef? GetOffsetTileRef(EntityUid uid, EntityStorageComponent component)
    {
        var targetCoordinates = new EntityCoordinates(uid, component.EnteringOffset).ToMap(EntityManager);

        if (_map.TryFindGridAt(targetCoordinates, out var grid))
        {
            return grid.GetTileRef(targetCoordinates);
        }

        return null;
    }

    private void OnRemoved(EntityUid uid, InsideEntityStorageComponent component, EntGotRemovedFromContainerMessage args)
    {
        if (args.Container.Owner != component.Storage)
            return;
        RemComp(uid, component);
    }

    #region Gas mix event handlers

    private void OnInsideInhale(EntityUid uid, InsideEntityStorageComponent component, InhaleLocationEvent args)
    {
        if (TryComp<EntityStorageComponent>(component.Storage, out var storage) && storage.Airtight)
        {
            args.Gas = storage.Air;
        }
    }

    private void OnInsideExhale(EntityUid uid, InsideEntityStorageComponent component, ExhaleLocationEvent args)
    {
        if (TryComp<EntityStorageComponent>(component.Storage, out var storage) && storage.Airtight)
        {
            args.Gas = storage.Air;
        }
    }

    private void OnInsideExposed(EntityUid uid, InsideEntityStorageComponent component, ref AtmosExposedGetAirEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<EntityStorageComponent>(component.Storage, out var storage))
        {
            if (!storage.Airtight)
                return;

            args.Gas = storage.Air;
        }

        args.Handled = true;
    }

    #endregion
}
