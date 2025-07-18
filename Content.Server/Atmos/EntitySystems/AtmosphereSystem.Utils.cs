using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Content.Server.Atmos.Components;
using Content.Server.Maps;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Maps;
using Content.Shared.Radiation.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;

namespace Content.Server.Atmos.EntitySystems;

public partial class AtmosphereSystem
{
    /// <summary>
    /// Gets the particular price of an air mixture.
    /// </summary>
    public double GetPrice(GasMixture mixture)
    {
        float basePrice = 0; // moles of gas * price/mole
        float totalMoles = 0; // total number of moles in can
        float maxComponent = 0; // moles of the dominant gas
        for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            basePrice += mixture.Moles[i] * GetGas(i).PricePerMole;
            totalMoles += mixture.Moles[i];
            maxComponent = Math.Max(maxComponent, mixture.Moles[i]);
        }

        // Pay more for gas canisters that are more pure
        float purity = 1;
        if (totalMoles > 0)
        {
            purity = maxComponent / totalMoles;
        }

        return basePrice * purity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InvalidateVisuals(Entity<GasTileOverlayComponent?> grid, Vector2i tile)
    {
        _gasTileOverlaySystem.Invalidate(grid, tile);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InvalidateVisuals(
        Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
        TileAtmosphere tile)
    {
        _gasTileOverlaySystem.Invalidate((ent.Owner, ent.Comp2), tile.GridIndices);
    }

    /// <summary>
    ///     Gets the volume in liters for a number of tiles, on a specific grid.
    /// </summary>
    /// <param name="mapGrid">The grid in question.</param>
    /// <param name="tiles">The amount of tiles.</param>
    /// <returns>The volume in liters that the tiles occupy.</returns>
    private float GetVolumeForTiles(MapGridComponent mapGrid, int tiles = 1)
    {
        return Atmospherics.CellVolume * mapGrid.TileSize * tiles;
    }

    public readonly record struct AirtightData(AtmosDirection BlockedDirections, bool NoAirWhenBlocked,
        bool FixVacuum);

    private void UpdateAirtightData(EntityUid uid, GridAtmosphereComponent atmos, MapGridComponent grid, TileAtmosphere tile)
    {
        var oldBlocked = tile.AirtightData.BlockedDirections;

        tile.AirtightData = tile.NoGridTile
            ? default
            : GetAirtightData(uid, grid, tile.GridIndices);

        if (tile.AirtightData.BlockedDirections != oldBlocked && tile.ExcitedGroup != null)
            ExcitedGroupDispose(atmos, tile.ExcitedGroup);
    }

    private AirtightData GetAirtightData(EntityUid uid, MapGridComponent grid, Vector2i tile)
    {
        var blockedDirs = AtmosDirection.Invalid;
        var noAirWhenBlocked = false;
        var fixVacuum = false;

        foreach (var ent in _map.GetAnchoredEntities(uid, grid, tile))
        {
            if (!_airtightQuery.TryGetComponent(ent, out var airtight))
                continue;

            fixVacuum |= airtight.FixVacuum;

            if (!airtight.AirBlocked)
                continue;

            blockedDirs |= airtight.AirBlockedDirection;
            noAirWhenBlocked |= airtight.NoAirWhenFullyAirBlocked;

            if (blockedDirs == AtmosDirection.All && noAirWhenBlocked && fixVacuum)
                break;
        }

        return new AirtightData(blockedDirs, noAirWhenBlocked, fixVacuum);
    }

    /// <summary>
    ///     Pries a tile in a grid.
    /// </summary>
    /// <param name="mapGrid">The grid in question.</param>
    /// <param name="tile">The indices of the tile.</param>
    private void PryTile(Entity<MapGridComponent> mapGrid, Vector2i tile)
    {
        if (!_mapSystem.TryGetTileRef(mapGrid.Owner, mapGrid.Comp, tile, out var tileRef))
            return;

        _tile.PryTile(tileRef);
    }

    /// <summary>
    ///     Possibly gets the coordinates of an optionally given <see cref="EntityUid">,
    ///     and then an optionally given <see cref="IGasMixtureHolder"/>.
    /// </summary>
    // Both args are nullable because this is exposed for use by reactions to get the position of the reaction.
    public bool TryGetMixtureHolderCoordinates(IGasMixtureHolder? holder, EntityUid? holderEntity, [NotNullWhen(true)] out EntityCoordinates? holderCoordinates)
    {
        if (holderEntity != null)
        {
            holderCoordinates = Transform(holderEntity.Value).Coordinates;
            return true;
        }

        if (holder is PipeNode pipeNode)
        {
            holderCoordinates = Transform(pipeNode.Owner).Coordinates;
            return true;
        }

        if (holder is TileAtmosphere tileAtmosphere && _mapGridQuery.TryComp(tileAtmosphere.GridIndex, out var mapGridComponent))
        {
            holderCoordinates = _mapSystem.GridTileToLocal(tileAtmosphere.GridIndex, mapGridComponent, tileAtmosphere.GridIndices);
            return true;
        }

        holderCoordinates = null;
        return false;
    }

    /// <summary>
    ///     If the provided mixture has a reaction entity corresponding
    ///     to the <paramref name="key"/>, returns that. Otherwise,
    ///     returns a newly spawned entity as the given
    ///     <paramref name="protoId"/>, attached to the given
    ///     <paramref name="coordinates"/>, and sets the mixture's
    ///     <see cref="GasMixture.ReactionEntities"/> accordingly.
    /// </summary>
    /// <remarks>
    ///     <see cref="GasMixture.ReactionEntities"/> must have an index
    ///     for the provided key.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityUid EnsureMixtureEntity(GasMixture mixture, byte key, EntProtoId protoId, EntityCoordinates coordinates)
    {
        ref EntityUid? entityElement = ref mixture.ReactionEntities[key];
        if (entityElement is { } keyEntity && Exists(keyEntity))
            return keyEntity;

        var entity = Spawn(protoId, coordinates);
        entityElement = entity;

        return entity;
    }

    /// <summary>
    ///     Set's the lifetime of an entity's <see cref="TimedDespawnComponent"/>,
    ///     if it exists, to <paramref name="newLifetime"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RefreshEntityTimedDespawn(EntityUid uid, float newLifetime)
    {
        if (TryComp<TimedDespawnComponent>(uid, out var timedDespawnComponent))
            timedDespawnComponent.Lifetime = newLifetime;
    }

    /// <summary>
    ///     Sets the radiation intensity and color of an entity if
    ///     it has the required <see cref="RadiationSourceComponent"/>
    ///     and <see cref="PointLightComponent"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AdjustRadiationPulse(EntityUid uid, float radiationIntensity, Color color, float lightEnergy)
    {
        if (TryComp<RadiationSourceComponent>(uid, out var radiationSourceComponent))
            radiationSourceComponent.Intensity = radiationIntensity;

        if (TryComp<PointLightComponent>(uid, out var pointLightComponent))
        {
            var visible = lightEnergy > 0.1f;

            _pointLightSystem.SetEnabled(uid, visible, pointLightComponent);
            if (!visible)
                return;

            _pointLightSystem.SetColor(uid, color, pointLightComponent);
            _pointLightSystem.SetEnergy(uid, lightEnergy, pointLightComponent);
        }
    }
}
