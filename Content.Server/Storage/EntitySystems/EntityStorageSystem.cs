using Content.Server.Atmos.EntitySystems;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Storage.Components;
using Content.Server.Tools.Systems;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Server.Storage.EntitySystems;

public sealed class EntityStorageSystem : SharedEntityStorageSystem
{
    [Dependency] private readonly ConstructionSystem _construction = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly IMapManager _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityStorageComponent, WeldableAttemptEvent>(OnWeldableAttempt);
        SubscribeLocalEvent<EntityStorageComponent, WeldableChangedEvent>(OnWelded);

        SubscribeLocalEvent<InsideEntityStorageComponent, InhaleLocationEvent>(OnInsideInhale);
        SubscribeLocalEvent<InsideEntityStorageComponent, ExhaleLocationEvent>(OnInsideExhale);
        SubscribeLocalEvent<InsideEntityStorageComponent, AtmosExposedGetAirEvent>(OnInsideExposed);

        SubscribeLocalEvent<InsideEntityStorageComponent, EntGotRemovedFromContainerMessage>(OnRemoved);
    }

    protected override void OnInit(EntityUid uid, SharedEntityStorageComponent component, ComponentInit args)
    {
        base.OnInit(uid, component, args);

        if (TryComp<ConstructionComponent>(uid, out var construction))
            _construction.AddContainer(uid, ContainerName, construction);

        if (!component.Open)
        {
            // If we're closed on spawn, we need to pull some air into our environment from where we spawned,
            // so that we have -something-. For example, if you bought an animal crate or something.
            TakeGas(uid, (EntityStorageComponent) component);
        }
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

    private void OnWelded(EntityUid uid, EntityStorageComponent component, WeldableChangedEvent args)
    {
        component.IsWeldedShut = args.IsWelded;
        Dirty(component);
    }

    protected override void TakeGas(EntityUid uid, SharedEntityStorageComponent component)
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

    public override void ReleaseGas(EntityUid uid, SharedEntityStorageComponent component)
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
