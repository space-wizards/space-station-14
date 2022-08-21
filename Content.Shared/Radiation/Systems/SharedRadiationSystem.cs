using Content.Shared.Radiation.Components;
using Robust.Shared.Map;

namespace Content.Shared.Radiation.Systems;

public abstract partial class SharedRadiationSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;

    private readonly Direction[] _directions =
    {
        Direction.North, Direction.South, Direction.East, Direction.West,
    };

    private readonly Direction[] _otherDirections =
    {
        Direction.NorthEast, Direction.NorthWest, Direction.SouthEast, Direction.SouthWest
    };

    public MapId MapId;
    public EntityUid gridUid;
    public Dictionary<Vector2i, float> visitedTiles = new();

    public override void Initialize()
    {
        base.Initialize();
        InitRadBlocking();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateRadSources();

    }


}
