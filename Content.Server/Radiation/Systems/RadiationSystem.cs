using Content.Server.FloodFill;
using Content.Shared.CCVar;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Events;
using Content.Shared.Radiation.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Radiation.Systems;

public sealed partial class RadiationSystem : SharedRadiationSystem
{
    private const float Slope = 1f;

    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly FloodFillSystem _floodFill = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public Dictionary<EntityUid, Dictionary<Vector2i, float>> _radiationMap = new();
    public List<(Matrix3, Dictionary<Vector2i, float>)> _spaceMap = new();

    private const float RadiationCooldown = 1.0f;
    private float _accumulator;

    private RadiationEngine _engine;

    public override void Initialize()
    {
        base.Initialize();
        InitRadBlocking();

        _cfg.OnValueChanged(CCVars.RadiationEngine, SetEngine, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(CCVars.RadiationEngine, SetEngine);
    }

    private void SetEngine(string engineName)
    {
        switch (engineName)
        {
            case "range":
                _engine = RadiationEngine.Range;
                break;
            case "grid":
                _engine = RadiationEngine.Grid;
                break;
            case "raycast":
                _engine = RadiationEngine.Raycast;
                break;
            default:
                Logger.Error($"Unknown radiation engine {engineName}");
                break;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _accumulator += frameTime;

        while (_accumulator > RadiationCooldown)
        {
            _accumulator -= RadiationCooldown;

            switch (_engine)
            {
                case RadiationEngine.Range:
                    UpdateOld();
                    break;
                case RadiationEngine.Grid:
                    UpdateRadSources();
                    UpdateReceivers();
                    break;
                case RadiationEngine.Raycast:
                    RaycastUpdate();
                    break;
            }
        }
    }


    private void UpdateRadSources()
    {
        foreach (var (_, map) in _radiationMap)
        {
            map.Clear();
        }
        _spaceMap.Clear();

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        foreach (var comp in EntityManager.EntityQuery<RadiationSourceComponent>())
        {
            var ent = comp.Owner;
            var cords = Transform(ent).MapPosition;
            CalculateRadiationMap(cords, comp.RadsPerSecond);
        }
        Logger.Info($"Generated radiation map {stopwatch.Elapsed.TotalMilliseconds}ms");

        RaiseNetworkEvent(new RadiationUpdate(_radiationMap, _spaceMap));

    }

    public void CalculateRadiationMap(MapCoordinates epicenter, float radsPerSecond)
    {


        var ff = _floodFill.DoFloodTile(epicenter,
            radsPerSecond,
            Slope,
            float.MaxValue,
            _resistancePerTile,
            0,
            100000,
            1000000
        );

        if (ff == null)
            return;


        foreach (var (gridUid, gridFlood) in ff.GridData)
        {
            if (!_radiationMap.ContainsKey(gridUid))
                _radiationMap.Add(gridUid, new Dictionary<Vector2i, float>());
            var dict = _radiationMap[gridUid];

            foreach (var (iter, poses) in gridFlood.TileLists)
            {
                var rads = ff.IterationIntensity[iter];
                foreach (var pos in poses)
                {
                    var r = rads;
                    if (dict.TryGetValue(pos, out var rad))
                        r += rad;

                    dict[pos] = r;
                }
            }
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
    }

    private void UpdateReceivers()
    {
        foreach (var receiver in EntityQuery<RadiationReceiverComponent>())
        {
            float rads = 0;

            var mapCoordinates = Transform(receiver.Owner).MapPosition;
            if (_mapManager.TryFindGridAt(mapCoordinates, out var candidateGrid) &&
                candidateGrid.TryGetTileRef(candidateGrid.WorldToTile(mapCoordinates.Position), out var tileRef))
            {
                var gridUid = tileRef.GridUid;
                var pos = tileRef.GridIndices;
                if (!_radiationMap.TryGetValue(gridUid, out var map) || !map.TryGetValue(pos, out rads))
                    continue;
            }
            else
            {
                foreach (var (mat, map) in _spaceMap)
                {
                    var invMat = mat.Invert();
                    var pos = invMat * mapCoordinates.Position;
                    var gridPos = new Vector2i(
                        (int)Math.Floor(pos.X),
                        (int)Math.Floor(pos.Y));

                    if (map.TryGetValue(gridPos, out var r))
                        rads += r;
                }
            }

            if (rads == 0)
                continue;

            var ev = new OnIrradiatedEvent(RadiationCooldown, rads);
            RaiseLocalEvent(receiver.Owner, ev);
        }
    }
}

public enum RadiationEngine
{
    Range,
    Grid,
    Raycast
}
