using Robust.Shared.Map;

namespace Content.Shared.Radiation.Systems;

public partial class SharedRadiationSystem
{
    private const float Slope = 1f;

    public Dictionary<EntityUid, Dictionary<Vector2i, float>> _radiationMap = new();

    private readonly Direction[] _directions =
    {
        Direction.North, Direction.South, Direction.East, Direction.West,
    };

    private void UpdateRadSources()
    {
        foreach (var (_, map) in _radiationMap)
        {
            map.Clear();
        }

        foreach (var comp in EntityManager.EntityQuery<RadiationSourceComponent>())
        {
            var ent = comp.Owner;
            var cords = Transform(ent).MapPosition;
            CalculateRadiationMap(cords, comp.RadsPerSecond);
        }
    }

    public void CalculateRadiationMap(MapCoordinates epicenter, float radsPerSecond)
    {
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

    }
}
