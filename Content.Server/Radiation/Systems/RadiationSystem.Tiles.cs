using System.Runtime.InteropServices;
using Content.Server.Radiation.Components;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Radiation.Systems;

public partial class RadiationSystem
{
    public sealed class TileSourceData
    {
        public float Intensity;
        public float Slope;
        public float HalfLife;
        public ushort SourceId = 0;
    }

    public readonly record struct SpatialTileKey(EntityUid GridUid, Vector2i Tile);

    private readonly Dictionary<SpatialTileKey, Dictionary<ushort, TileSourceData>> _tileRadiationSources = [];
    private readonly ValueList<SpatialTileKey> _expiredKeys = [];
    private readonly ValueList<ushort> _expiredSources = [];

    public void SetTileRadiation(EntityUid gridUid, Vector2i tile, ushort sourceId, float intensity, float slope, float halfLife = -1f, bool forceSet = false)
    {
        if (intensity < MinIntensity)
            return;

        var key = new SpatialTileKey(gridUid, tile);

        ref var tileSources = ref CollectionsMarshal.GetValueRefOrAddDefault(_tileRadiationSources, key, out var exists);
        if (!exists || tileSources == null)
        {
            tileSources = new Dictionary<ushort, TileSourceData>();
        }

        ref var source = ref CollectionsMarshal.GetValueRefOrAddDefault(tileSources, sourceId, out var sourceExists);
        if (!sourceExists || source == null)
        {
            source = new TileSourceData
            {
                Intensity = intensity,
                Slope = slope,
                HalfLife = halfLife,
                SourceId = sourceId
            };
        }
        else
        {
            source.Intensity = forceSet ? intensity : MathF.Max(source.Intensity, intensity);
            source.Slope = slope;
            source.HalfLife = halfLife;
        }
    }

    private void UpdateTileRadiationSources(float seconds)
    {
        _expiredKeys.Clear();

        var lastGridUid = EntityUid.Invalid;
        MapGridComponent? lastGridComp = null;

        foreach (var (spatialKey, tileSources) in _tileRadiationSources)
        {
            var gridUid = spatialKey.GridUid;

            if (gridUid != lastGridUid)
            {
                lastGridUid = gridUid;
                _gridQuery.TryGetComponent(gridUid, out lastGridComp);
            }

            if (lastGridComp == null ||
                !_maps.TryGetTileRef(gridUid, lastGridComp, spatialKey.Tile, out var tileRef) ||
                tileRef.Tile.IsEmpty)
            {
                _expiredKeys.Add(spatialKey);
                continue;
            }

            _expiredSources.Clear();

            foreach (var (sourceId, source) in tileSources)
            {
                if (source.HalfLife < 0f)
                {
                    _expiredSources.Add(sourceId);
                    continue;
                }

                if (source.HalfLife > 0f)
                {
                    source.Intensity *= MathF.Pow(0.5f, seconds / source.HalfLife);
                }

                if (source.Intensity < MinIntensity)
                {
                    _expiredSources.Add(sourceId);
                }
            }

            for (var i = 0; i < _expiredSources.Count; i++)
            {
                tileSources.Remove(_expiredSources[i]);
            }

            if (tileSources.Count == 0)
            {
                _expiredKeys.Add(spatialKey);
            }
        }

        for (var i = 0; i < _expiredKeys.Count; i++)
        {
            _tileRadiationSources.Remove(_expiredKeys[i]);
        }
    }

    // Only used for testing currently. If there is any reason to keep it, it should
    // be rewritten to be more performant as radSys Update() calls it every second.
    private void UpdateTileRadiationEmitters()
    {
        var query = EntityQueryEnumerator<TileRadiationEmitterComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var emitter, out var xform))
        {
            if (!emitter.Enabled)
                continue;

            var gridUid = xform.GridUid;
            if (gridUid == null || !_gridQuery.TryGetComponent(gridUid.Value, out var grid))
                continue;

            var tileIndices = _maps.TileIndicesFor((gridUid.Value, grid), xform.Coordinates);
            var totalIntensity = emitter.Intensity * _stack.GetCount(uid);
            var sourceId = (ushort)uid;

            SetTileRadiation(
                gridUid.Value,
                tileIndices,
                sourceId,
                totalIntensity,
                emitter.Slope,
                emitter.HalfLife
            );
        }
    }

    public void ClearTileRadiation(EntityUid gridUid, Vector2i tile, Func<TileSourceData, bool> condition)
    {
        var key = new SpatialTileKey(gridUid, tile);
        if (!_tileRadiationSources.TryGetValue(key, out var tileSources))
            return;

        var expiredSources = new ValueList<ushort>();

        foreach (var (sourceId, source) in tileSources)
        {
            if (condition(source))
            {
                expiredSources.Add(sourceId);
            }
        }

        foreach (var sourceId in expiredSources)
        {
            tileSources.Remove(sourceId);
        }

        if (tileSources.Count == 0)
        {
            _tileRadiationSources.Remove(key);
        }
    }

    public void ClearAllTileRadiation(Func<TileSourceData, bool> condition)
    {
        var expiredKeys = new ValueList<SpatialTileKey>();

        foreach (var (spatialKey, tileSources) in _tileRadiationSources)
        {
            var expiredSources = new ValueList<ushort>();

            foreach (var (sourceId, source) in tileSources)
            {
                if (condition(source))
                {
                    expiredSources.Add(sourceId);
                }
            }

            foreach (var sourceId in expiredSources)
            {
                tileSources.Remove(sourceId);
            }

            if (tileSources.Count == 0)
            {
                expiredKeys.Add(spatialKey);
            }
        }

        foreach (var key in expiredKeys)
        {
            _tileRadiationSources.Remove(key);
        }
    }

    public IReadOnlyDictionary<SpatialTileKey, Dictionary<ushort, TileSourceData>> GetTileRadiationSources()
    {
        return _tileRadiationSources;
    }
}
