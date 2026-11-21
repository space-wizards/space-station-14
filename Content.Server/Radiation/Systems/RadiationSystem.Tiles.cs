using System.Runtime.InteropServices;
using Content.Server.Radiation.Components;
using Robust.Shared.Map.Components;

namespace Content.Server.Radiation.Systems;

public partial class RadiationSystem
{
    public static readonly float HalfLifeConstantFactor = MathF.Log(2f);
    private readonly List<(Vector2i Tile, ushort SourceId)> _toRemove = [];
    public void SetTileRadiation(EntityUid gridUid, Vector2i tile, ushort sourceId, float intensity, float slope, float halfLife = -1f, bool forceSet = false)
    {
        if (intensity < MinIntensity || !_entMan.EntityExists(gridUid))
            return;

        var gridRadComp = _entMan.EnsureComponent<GridTileRadiationComponent>(gridUid);
        var key = (tile, sourceId);
        ref var source = ref CollectionsMarshal.GetValueRefOrAddDefault(gridRadComp.Sources, key, out var sourceExists);

        if (sourceExists)
        {
            source.Intensity = forceSet ? intensity : MathF.Max(source.Intensity, intensity);
            source.Slope = slope;
            source.HalfLife = halfLife;
        }
        else
        {
            source = new TileSourceData(tile, sourceId, intensity, slope, halfLife);
        }
    }

    private void UpdateTileRadiationSources(float seconds)
    {
        var query = EntityQueryEnumerator<GridTileRadiationComponent, MapGridComponent>();
        while (query.MoveNext(out var gridUid, out var gridRad, out var gridComp))
        {
            var sources = gridRad.Sources;

            foreach (var (key, source) in sources)
            {
                var updatedSource = source;

                if (updatedSource.HalfLife == 0f)
                    continue;

                if (updatedSource.HalfLife < 0f)
                {
                    _toRemove.Add(key);
                    continue;
                }

                if (updatedSource.HalfLife > 0f)
                {
                    updatedSource.Intensity *= MathF.Exp(seconds * (-HalfLifeConstantFactor / updatedSource.HalfLife));
                }

                if (updatedSource.Intensity < MinIntensity ||
                    !_maps.TryGetTileRef(gridUid, gridComp, updatedSource.Tile, out var tileRef) ||
                    tileRef.Tile.IsEmpty)
                {
                    _toRemove.Add(key);
                    continue;
                }

                CollectionsMarshal.GetValueRefOrAddDefault(sources, key, out _) = updatedSource;
            }

            for (var i = 0; i < _toRemove.Count; i++)
            {
                sources.Remove(_toRemove[i]);
            }

            _toRemove.Clear();

            if (sources.Count == 0)
            {
                RemCompDeferred<GridTileRadiationComponent>(gridUid);
            }
        }
    }

    public void ClearTileRadiation(EntityUid gridUid, Vector2i tile, Func<TileSourceData, bool> condition)
    {
        if (!_entMan.TryGetComponent<GridTileRadiationComponent>(gridUid, out var gridRad))
            return;

        foreach (var (key, source) in gridRad.Sources)
        {
            if (source.Tile == tile && condition(source))
            {
                _toRemove.Add(key);
            }
        }

        for (var i = 0; i < _toRemove.Count; i++)
        {
            gridRad.Sources.Remove(_toRemove[i]);
        }

        _toRemove.Clear();
    }

    public void ClearAllTileRadiation(Func<TileSourceData, bool> condition)
    {
        var query = EntityQueryEnumerator<GridTileRadiationComponent>();
        while (query.MoveNext(out var gridUid, out var gridRad))
        {
            foreach (var (key, source) in gridRad.Sources)
            {
                if (condition(source))
                {
                    _toRemove.Add(key);
                }
            }

            for (var i = 0; i < _toRemove.Count; i++)
            {
                gridRad.Sources.Remove(_toRemove[i]);
            }

            _toRemove.Clear();
        }
    }
}
