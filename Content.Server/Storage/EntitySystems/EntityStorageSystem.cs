using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Storage.Components;
using Content.Shared.Destructible;
using Content.Shared.Explosion;
using Content.Shared.Foldable;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Content.Shared.Movement.Events;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Server.Storage.EntitySystems;

public sealed class EntityStorageSystem : SharedEntityStorageSystem
{
    [Dependency] private readonly ConstructionSystem _construction = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        /* CompRef things */
        SubscribeLocalEvent<EntityStorageComponent, EntityUnpausedEvent>(OnEntityUnpausedEvent);
        SubscribeLocalEvent<EntityStorageComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<EntityStorageComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<EntityStorageComponent, ActivateInWorldEvent>(OnInteract, after: new[] { typeof(LockSystem) });
        SubscribeLocalEvent<EntityStorageComponent, LockToggleAttemptEvent>(OnLockToggleAttempt);
        SubscribeLocalEvent<EntityStorageComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<EntityStorageComponent, GetVerbsEvent<InteractionVerb>>(AddToggleOpenVerb);
        SubscribeLocalEvent<EntityStorageComponent, ContainerRelayMovementEntityEvent>(OnRelayMovement);
        SubscribeLocalEvent<EntityStorageComponent, FoldAttemptEvent>(OnFoldAttempt);

        SubscribeLocalEvent<EntityStorageComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<EntityStorageComponent, ComponentHandleState>(OnHandleState);
        /* CompRef things */

        SubscribeLocalEvent<EntityStorageComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EntityStorageComponent, WeldableAttemptEvent>(OnWeldableAttempt);
        SubscribeLocalEvent<EntityStorageComponent, BeforeExplodeEvent>(OnExploded);

        SubscribeLocalEvent<InsideEntityStorageComponent, InhaleLocationEvent>(OnInsideInhale);
        SubscribeLocalEvent<InsideEntityStorageComponent, ExhaleLocationEvent>(OnInsideExhale);
        SubscribeLocalEvent<InsideEntityStorageComponent, AtmosExposedGetAirEvent>(OnInsideExposed);

        SubscribeLocalEvent<InsideEntityStorageComponent, EntGotRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnMapInit(EntityUid uid, EntityStorageComponent component, MapInitEvent args)
    {
        if (!component.Open && component.Air.TotalMoles == 0)
        {
            // If we're closed on spawn and have no air already saved, we need to pull some air into our environment from where we spawned,
            // so that we have -something-. For example, if you bought an animal crate or something.
            TakeGas(uid, component);
        }
    }

    protected override void OnComponentInit(EntityUid uid, EntityStorageComponent component, ComponentInit args)
    {
        base.OnComponentInit(uid, component, args);

        if (TryComp<ConstructionComponent>(uid, out var construction))
            _construction.AddContainer(uid, ContainerName, construction);
    }

    public override bool ResolveStorage(EntityUid uid, [NotNullWhen(true)] ref EntityStorageComponent? component)
    {
        if (component != null)
            return true;

        TryComp<EntityStorageComponent>(uid, out var storage);
        component = storage;
        return component != null;
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
            Popup.PopupEntity(msg, args.User, args.User);
            args.Cancel();
        }
    }

    private void OnExploded(Entity<EntityStorageComponent> ent, ref BeforeExplodeEvent args)
    {
        args.Contents.AddRange(ent.Comp.Contents.ContainedEntities);
    }

    protected override void TakeGas(EntityUid uid, EntityStorageComponent component)
    {
        if (!component.Airtight)
            return;

        var serverComp = (EntityStorageComponent) component;
        var tile = GetOffsetTileRef(uid, serverComp);

        if (tile != null && _atmos.GetTileMixture(tile.Value.GridUid, null, tile.Value.GridIndices, true) is {} environment)
        {
            _atmos.Merge(serverComp.Air, environment.RemoveVolume(serverComp.Air.Volume));
        }
    }

    public override void ReleaseGas(EntityUid uid, EntityStorageComponent component)
    {
        var serverComp = (EntityStorageComponent) component;

        if (!serverComp.Airtight)
            return;

        var tile = GetOffsetTileRef(uid, serverComp);

        if (tile != null && _atmos.GetTileMixture(tile.Value.GridUid, null, tile.Value.GridIndices, true) is {} environment)
        {
            _atmos.Merge(environment, serverComp.Air);
            serverComp.Air.Clear();
        }
    }

    private TileRef? GetOffsetTileRef(EntityUid uid, EntityStorageComponent component)
    {
        var targetCoordinates = TransformSystem.ToMapCoordinates(new EntityCoordinates(uid, component.EnteringOffset));

        if (_map.TryFindGridAt(targetCoordinates, out var gridId, out var grid))
        {
            return _mapSystem.GetTileRef(gridId, grid, targetCoordinates);
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
