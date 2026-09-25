using System.Runtime.InteropServices;
using Content.Server.Radiation.Components;
using Robust.Shared.Collections;
using Robust.Shared.Map;

namespace Content.Server.Radiation.Systems;

public partial class RadiationSystem
{
    public sealed class TileSourceData
    {
        public float Intensity;
        public float Slope;
        public float HalfLife;
        public string SourceId = string.Empty;
    }

    public readonly record struct SpatialTileKey(EntityUid GridUid, Vector2i Tile);

    private readonly Dictionary<SpatialTileKey, Dictionary<string, TileSourceData>> _tileRadiationSources = new();

    public void SetTileRadiation(EntityUid gridUid, Vector2i tile, string sourceId, float intensity, float slope, float halfLife = -1f, bool forceSet = false)
    {
        if (intensity < MinIntensity)
            return;

        var key = new SpatialTileKey(gridUid, tile);

        ref var tileSources = ref CollectionsMarshal.GetValueRefOrAddDefault(_tileRadiationSources, key, out var exists);
        if (!exists || tileSources == null)
        {
            tileSources = new Dictionary<string, TileSourceData>();
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

    private void UpdateTileHalfLives()
    {
        var expiredKeys = new ValueList<SpatialTileKey>();

        foreach (var (spatialKey, tileSources) in _tileRadiationSources)
        {
            var expiredSources = new ValueList<string>();

            foreach (var (sourceId, source) in tileSources)
            {
                if (source.HalfLife < 0f)
                {
                    expiredSources.Add(sourceId);
                    continue;
                }

                if (source.HalfLife > 0f)
                {
                    source.Intensity *= MathF.Pow(0.5f, GridcastUpdateRate / source.HalfLife);
                }

                if (source.Intensity < MinIntensity)
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

    private void UpdateTileEmitters()
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
            var sourceId = uid.ToString();

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

        var expiredSources = new ValueList<string>();

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
            var expiredSources = new ValueList<string>();

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

    public IReadOnlyDictionary<SpatialTileKey, Dictionary<string, TileSourceData>> GetTileRadiationSources()
    {
        return _tileRadiationSources;
    }
}
