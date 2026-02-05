using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Server.GameObjects;
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

        SubscribeLocalEvent<EntityStorageComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<InsideEntityStorageComponent, InhaleLocationEvent>(OnInsideInhale);
        SubscribeLocalEvent<InsideEntityStorageComponent, ExhaleLocationEvent>(OnInsideExhale);
        SubscribeLocalEvent<InsideEntityStorageComponent, AtmosExposedGetAirEvent>(OnInsideExposed);
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

    protected override void TakeGas(EntityUid uid, EntityStorageComponent component)
    {
        if (!component.Airtight)
            return;

        var tile = GetOffsetTileRef(uid, component);

        if (tile != null && _atmos.GetTileMixture(tile.Value.GridUid, null, tile.Value.GridIndices, true) is { } environment)
        {
            _atmos.Merge(component.Air, environment.RemoveVolume(component.Air.Volume));
        }
    }

    public override void ReleaseGas(EntityUid uid, EntityStorageComponent component)
    {
        if (!component.Airtight)
            return;

        var tile = GetOffsetTileRef(uid, component);

        if (tile != null && _atmos.GetTileMixture(tile.Value.GridUid, null, tile.Value.GridIndices, true) is { } environment)
        {
            _atmos.Merge(environment, component.Air);
            component.Air.Clear();
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
