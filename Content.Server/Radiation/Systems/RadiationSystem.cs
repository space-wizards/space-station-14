using Content.Shared.FloodFill;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Events;
using Content.Shared.Radiation.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Radiation.Systems;

public sealed class RadiationSystem : SharedRadiationSystem
{
    private const float Slope = 1f;

    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly FloodFillSystem _floodFill = default!;

    public Dictionary<EntityUid, Dictionary<Vector2i, float>> _radiationMap = new();
    public List<(Matrix3, Dictionary<Vector2i, float>)> _spaceMap = new();

    private const float RadiationCooldown = 1.0f;
    private float _accumulator;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _accumulator += frameTime;

        while (_accumulator > RadiationCooldown)
        {
            _accumulator -= RadiationCooldown;

            UpdateRadSources();
            UpdateReceivers();
        }
    }


    private void UpdateRadSources()
    {
        foreach (var (_, map) in _radiationMap)
        {
            map.Clear();
        }
        _spaceMap.Clear();

        foreach (var comp in EntityManager.EntityQuery<RadiationSourceComponent>())
        {
            var ent = comp.Owner;
            var cords = Transform(ent).MapPosition;
            CalculateRadiationMap(cords, comp.RadsPerSecond);
        }
    }

    public void CalculateRadiationMap(MapCoordinates epicenter, float radsPerSecond)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var ff = _floodFill.DoFloodTile(epicenter,
            0,
            radsPerSecond,
            Slope,
            float.MaxValue,
            _resistancePerTile,
            100000,
            1000000
        );

        if (ff == null)
            return;
        Logger.Info($"Generated radiation map with {ff.Area} tiles in {stopwatch.Elapsed.TotalMilliseconds}ms");

        foreach (var (gridUid, gridFlood) in ff.GridData)
        {
            var dict = new Dictionary<Vector2i, float>();
            foreach (var (iter, poses) in gridFlood.TileLists)
            {
                var rads = ff.IterationIntensity[iter];
                foreach (var pos in poses)
                {
                    dict[pos] = rads;
                }
            }

            _radiationMap[gridUid] = dict;
        }

        if (ff.SpaceData != null)
        {
            var dict = new Dictionary<Vector2i, float>();
            foreach (var (iter, poses) in ff.SpaceData.TileLists)
            {
                var rads = ff.IterationIntensity[iter];
                foreach (var pos in poses)
                {
                    dict[pos] = rads;
                }
            }

            _spaceMap.Add((ff.SpaceMatrix, dict));
        }

        RaiseNetworkEvent(new RadiationUpdate(_radiationMap, _spaceMap));
    }

    private void UpdateReceivers()
    {
        foreach (var receiver in EntityQuery<RadiationReceiverComponent>())
        {
            var mapCoordinates = Transform(receiver.Owner).MapPosition;
            if (!_mapManager.TryFindGridAt(mapCoordinates, out var candidateGrid) ||
                !candidateGrid.TryGetTileRef(candidateGrid.WorldToTile(mapCoordinates.Position), out var tileRef))
            {
                return;
            }

            var gridUid = tileRef.GridUid;
            var pos = tileRef.GridIndices;
            if (!_radiationMap.TryGetValue(gridUid, out var map) ||
                !map.TryGetValue(pos, out var rads))
                return;

            var ev = new OnIrradiatedEvent(RadiationCooldown, rads);
            RaiseLocalEvent(receiver.Owner, ev);
        }
    }
}
