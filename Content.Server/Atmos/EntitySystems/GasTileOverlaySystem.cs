using System.Runtime.InteropServices;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.CCVar;
using Content.Shared.Rounding;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.EntitySystems;

[UsedImplicitly]
public sealed partial class GasTileOverlaySystem : SharedGasTileOverlaySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private ChunkEntitySystem _chunkEntity = default!;

    [Dependency] private EntityQuery<GridAtmosphereComponent> _gridAtmosphereQuery;
    [Dependency] private EntityQuery<GasOverlayChunkComponent> _overlayChunkQuery;

    /// <summary>
    ///     Overlay update interval, in seconds.
    /// </summary>
    private float _updateInterval;

    private int _thresholds;

    public override void Initialize()
    {
        base.Initialize();
        InitializeCVars();
    }

    private void UpdateTickRate(float value) => _updateInterval = value > 0.0f ? 1 / value : float.MaxValue;
    private void UpdateThresholds(int value) => _thresholds = value;

    public void Invalidate(Entity<GridAtmosphereComponent?> grid, Vector2i index)
    {
        if (!_gridAtmosphereQuery.Resolve(grid.Owner, ref grid.Comp))
            return;

        var chunkIndex = GetGasChunkIndices(index);
        var dataIndex = GasOverlayChunkComponent.GetDataIndex(chunkIndex, index);
        ref var mask = ref CollectionsMarshal.GetValueRefOrAddDefault(grid.Comp.InvalidOverlayChunks, chunkIndex, out _);
        mask.Add(dataIndex);
    }

    private byte GetOpacity(float moles, float molesVisible, float molesVisibleMax)
    {
        return (byte) (ContentHelpers.RoundToLevels(
            MathHelper.Clamp01((moles - molesVisible) /
                               (molesVisibleMax - molesVisible)) * 255, byte.MaxValue,
            _thresholds) * 255 / (_thresholds - 1));
    }

    public GasOverlayData GetOverlayData(GasMixture? mixture)
    {
        ThermalByte byteTemp;
        if (mixture == null)
        {
            byteTemp = new();
            byteTemp.SetVacuum();
        }
        else
            byteTemp = new(mixture.Temperature);

        var packedOpacity = 0UL;

        var visibleGasIndex = 0;
        for (var id = 0; id < Atmospherics.TotalNumberOfGases; id++)
        {
            if (!IsGasVisible(id))
                continue;

            var gas = _atmosphereSystem.GetGas(id);
            var moles = mixture?[id] ?? 0f;

            if (moles < gas.GasMolesVisible)
            {
                visibleGasIndex++;
                continue;
            }

            var opacity = GetOpacity(moles, gas.GasMolesVisible, gas.GasMolesVisibleMax);
            packedOpacity = GasOverlayOpacityData.SetOpacity(packedOpacity, visibleGasIndex, opacity);
            visibleGasIndex++;
        }

        return new GasOverlayData(0, packedOpacity, byteTemp);
    }

    /// <summary>
    ///     Updates the visuals for a tile on some grid chunk. Returns true if the visuals have changed.
    /// </summary>
    private bool UpdateChunkTile(
        GridAtmosphereComponent gridAtmosphere,
        Vector2i chunkIndex,
        GasOverlayChunkComponent chunk,
        Vector2i index)
    {
        var dataIndex = GasOverlayChunkComponent.GetDataIndex(chunkIndex, index);
        ref var oldFire = ref chunk.FireData[dataIndex];
        ref var oldOpacity = ref chunk.OpacityData[dataIndex];
        ref var oldTemperature = ref chunk.TemperatureData[dataIndex];

        if (!gridAtmosphere.Tiles.TryGetValue(index, out var tile))
        {
            var cleared = false;
            if (!oldFire.Equals(default))
            {
                oldFire = default;
                MarkFireModified(chunk, dataIndex);
                cleared = true;
            }

            if (!oldOpacity.Equals(default))
            {
                oldOpacity = default;
                MarkOpacityModified(chunk, dataIndex);
                cleared = true;
            }

            if (!oldTemperature.Equals(default))
            {
                oldTemperature = default;
                MarkTemperatureModified(chunk, dataIndex);
                cleared = true;
            }

            return cleared;
        }

        var changed = UpdateFireState(chunk, dataIndex, ref oldFire, tile);
        changed |= UpdateTemperature(chunk, dataIndex, ref oldTemperature, tile);
        changed |= UpdateOpacity(chunk, dataIndex, ref oldOpacity, tile);

        return changed;
    }

    private bool UpdateTemperature(
        GasOverlayChunkComponent chunk,
        int dataIndex,
        ref GasOverlayTemperatureData oldTemperature,
        TileAtmosphere tile)
    {
        ThermalByte newByteTemp = new();
        if (tile.Hotspot.Valid)
            newByteTemp.SetTemperature(tile.Hotspot.Temperature);
        else if (!tile.Space && tile.Air?.TotalMoles <= 5f)
            newByteTemp.SetVacuum();
        else if (!tile.Space && tile.Air != null)
            newByteTemp = new(tile.Air.Temperature);

        var oldByteTemp = oldTemperature.ByteGasTemperature;
        if (oldTemperature.Active &&
            Math.Abs(oldByteTemp.Value - newByteTemp.Value) <= 1 &&
            (oldByteTemp.Value == newByteTemp.Value || newByteTemp.Value <= ThermalByte.TempResolution))
        {
            return false;
        }

        oldTemperature = new GasOverlayTemperatureData(newByteTemp);
        MarkTemperatureModified(chunk, dataIndex);
        return true;
    }

    private bool UpdateFireState(
        GasOverlayChunkComponent chunk,
        int dataIndex,
        ref GasOverlayFireData oldFire,
        TileAtmosphere tile)
    {
        var newFire = new GasOverlayFireData(tile.Hotspot.State);
        if (oldFire.Equals(newFire))
            return false;

        oldFire = newFire;
        MarkFireModified(chunk, dataIndex);
        return true;
    }

    private bool UpdateOpacity(
        GasOverlayChunkComponent chunk,
        int dataIndex,
        ref GasOverlayOpacityData oldOpacityData,
        TileAtmosphere tile)
    {
        var packedOpacity = oldOpacityData.PackedOpacity;
        var changed = false;

        if (tile is {Air: not null, NoGridTile: false})
        {
            var visibleGasIndex = 0;
            for (var id = 0; id < Atmospherics.TotalNumberOfGases; id++)
            {
                if (!IsGasVisible(id))
                    continue;

                var gas = _atmosphereSystem.GetGas(id);
                var moles = tile.Air[id];
                var newOpacity = moles < gas.GasMolesVisible
                    ? (byte) 0
                    : GetOpacity(moles, gas.GasMolesVisible, gas.GasMolesVisibleMax);

                var oldOpacity = oldOpacityData.GetOpacity(visibleGasIndex);
                if (oldOpacity == newOpacity)
                {
                    visibleGasIndex++;
                    continue;
                }

                packedOpacity = GasOverlayOpacityData.SetOpacity(packedOpacity, visibleGasIndex, newOpacity);
                changed = true;
                visibleGasIndex++;
            }
        }
        else if (packedOpacity != 0)
        {
            packedOpacity = 0;
            changed = true;
        }

        if (!changed)
            return false;

        oldOpacityData = packedOpacity == 0
            ? default
            : new GasOverlayOpacityData(packedOpacity);
        MarkOpacityModified(chunk, dataIndex);
        return true;
    }

    private void MarkFireModified(GasOverlayChunkComponent chunk, int dataIndex)
    {
        chunk.FireLastModified[dataIndex] = _gameTiming.CurTick;
    }

    private void MarkOpacityModified(GasOverlayChunkComponent chunk, int dataIndex)
    {
        chunk.OpacityLastModified[dataIndex] = _gameTiming.CurTick;
    }

    private void MarkTemperatureModified(GasOverlayChunkComponent chunk, int dataIndex)
    {
        chunk.TemperatureLastModified[dataIndex] = _gameTiming.CurTick;
    }

    private static bool IsEmpty(GasOverlayChunkComponent chunk)
    {
        for (var i = 0; i < chunk.FireData.Length; i++)
        {
            if (!IsTileEmpty(chunk, i))
                return false;
        }

        return true;
    }

    private static bool IsTileEmpty(GasOverlayChunkComponent chunk, int dataIndex)
    {
        return chunk.FireData[dataIndex].Equals(default) &&
               chunk.OpacityData[dataIndex].Equals(default) &&
               chunk.TemperatureData[dataIndex].Equals(default);
    }

    private void UpdateOverlayData()
    {
        var query = AllEntityQuery<GridAtmosphereComponent>();
        while (query.MoveNext(out var gridUid, out var gam))
        {
            if (gam.InvalidOverlayChunks.Count == 0)
                continue;

            foreach (var (chunkIndex, invalidTiles) in gam.InvalidOverlayChunks)
            {
                if (!_chunkEntity.TryGetChunk(gridUid, chunkIndex, out var chunkEnt))
                {
                    if (!AnyTileExists(gam, chunkIndex, invalidTiles))
                        continue;

                    chunkEnt = _chunkEntity.GetOrCreateChunk(gridUid, chunkIndex);
                }

                var chunkUid = chunkEnt.Value.Owner;
                if (!_overlayChunkQuery.TryComp(chunkUid, out var chunk))
                    chunk = AddComp<GasOverlayChunkComponent>(chunkUid);

                var changed = false;
                var mayBeEmpty = false;
                var mask = invalidTiles;
                while (mask.TryPop(out var dataIndex))
                {
                    var index = GetGridIndices(chunkIndex, dataIndex);
                    if (!UpdateChunkTile(gam, chunkIndex, chunk, index))
                        continue;

                    changed = true;
                    mayBeEmpty |= IsTileEmpty(chunk, dataIndex);
                }

                if (!changed)
                    continue;

                if (mayBeEmpty && IsEmpty(chunk))
                {
                    RemComp<GasOverlayChunkComponent>(chunkUid);
                    _chunkEntity.TryRemoveChunk((chunkEnt.Value.Owner, chunkEnt.Value.Comp, null));
                    continue;
                }

                Dirty(chunkUid, chunk);
            }

            gam.InvalidOverlayChunks.Clear();
        }
    }

    private static bool AnyTileExists(GridAtmosphereComponent gam, Vector2i chunkIndex, GasOverlayInvalidTileMask invalidTiles)
    {
        var mask = invalidTiles;
        while (mask.TryPop(out var dataIndex))
        {
            var index = GetGridIndices(chunkIndex, dataIndex);
            if (gam.Tiles.ContainsKey(index))
                return true;
        }

        return false;
    }

    private static Vector2i GetGridIndices(Vector2i chunkIndex, int dataIndex)
    {
        var origin = chunkIndex * ChunkSize;
        return origin + new Vector2i(dataIndex % ChunkSize, dataIndex / ChunkSize);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        AccumulatedFrameTime += frameTime;

        if (AccumulatedFrameTime < _updateInterval)
            return;

        AccumulatedFrameTime -= _updateInterval;
        UpdateOverlayData();
    }

    private void InitializeCVars()
    {
        Subs.CVar(ConfMan, CCVars.NetGasOverlayTickRate, UpdateTickRate, true);
        Subs.CVar(ConfMan, CCVars.GasOverlayThresholds, UpdateThresholds, true);
    }
}
