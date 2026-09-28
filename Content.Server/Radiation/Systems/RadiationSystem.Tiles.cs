using Content.Server.Radiation.Components;
using Robust.Shared.Map.Components;

namespace Content.Server.Radiation.Systems;

public partial class RadiationSystem
{
    public void SetTileRadiation(EntityUid gridUid, Vector2i tile, ushort sourceId, float intensity, float slope, float halfLife = -1f, bool forceSet = false)
    {
        if (intensity < MinIntensity)
            return;

        if (!_entMan.EntityExists(gridUid))
            return;

        var gridRadComp = _entMan.EnsureComponent<GridTileRadiationComponent>(gridUid);

        for (var i = 0; i < gridRadComp.Sources.Count; i++)
        {
            var source = gridRadComp.Sources[i];
            if (source.Tile == tile && source.SourceId == sourceId)
            {
                source.Intensity = forceSet ? intensity : MathF.Max(source.Intensity, intensity);
                source.Slope = slope;
                source.HalfLife = halfLife;
                gridRadComp.Sources[i] = source;
                return;
            }
        }

        gridRadComp.Sources.Add(new TileSourceData(tile, sourceId, intensity, slope, halfLife));
    }

    private void UpdateTileRadiationSources(float seconds)
    {
        var query = EntityQueryEnumerator<GridTileRadiationComponent, MapGridComponent>();
        while (query.MoveNext(out var gridUid, out var gridRad, out var gridComp))
        {
            for (var i = gridRad.Sources.Count - 1; i >= 0; i--)
            {
                var updatedSource = gridRad.Sources[i];

                if (updatedSource.HalfLife == 0f)
                {
                    continue;
                }

                if (updatedSource.HalfLife < 0f)
                {
                    gridRad.Sources.RemoveAt(i);
                    continue;
                }

                if (updatedSource.HalfLife > 0f)
                {
                    updatedSource.Intensity *= MathF.Pow(0.5f, seconds / updatedSource.HalfLife);
                }

                if (updatedSource.Intensity < MinIntensity ||
                    !_maps.TryGetTileRef(gridUid, gridComp, updatedSource.Tile, out var tileRef) ||
                    tileRef.Tile.IsEmpty)
                {
                    gridRad.Sources.RemoveAt(i);
                    continue;
                }

                gridRad.Sources[i] = updatedSource;
            }
        }
    }

    public void ClearTileRadiation(EntityUid gridUid, Vector2i tile, Func<TileSourceData, bool> condition)
    {
        if (!_entMan.TryGetComponent<GridTileRadiationComponent>(gridUid, out var gridRad))
            return;

        for (var i = gridRad.Sources.Count - 1; i >= 0; i--)
        {
            var source = gridRad.Sources[i];
            if (source.Tile == tile && condition(source))
            {
                gridRad.Sources.RemoveAt(i);
            }
        }
    }

    public void ClearAllTileRadiation(Func<TileSourceData, bool> condition)
    {
        var query = EntityQueryEnumerator<GridTileRadiationComponent>();
        while (query.MoveNext(out var gridUid, out var gridRad))
        {
            for (var i = gridRad.Sources.Count - 1; i >= 0; i--)
            {
                if (condition(gridRad.Sources[i]))
                {
                    gridRad.Sources.RemoveAt(i);
                }
            }
        }
    }
}
