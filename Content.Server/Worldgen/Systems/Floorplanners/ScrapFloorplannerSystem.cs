using Content.Server.Worldgen.Floorplanners;

namespace Content.Server.Worldgen.Systems.Floorplanners;

/// <summary>
/// This handles...
/// </summary>
public sealed class ScrapFloorplannerSystem : FloorplanSystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {

    }

    public override bool ConstructTiling(FloorplanConfig config, EntityUid targetGrid, Vector2 centerPoint, Constraint? bounds,
        out object? planData)
    {

        planData = null;
        return true;
    }

    public override void Populate(FloorplanConfig config, EntityUid targetGrid, Vector2 centerPoint, Constraint? bounds,
        in object? planData)
    {
        throw new NotImplementedException();
    }

    private sealed class ScrapCorridorData
    {
        public HashSet<Vector2> WallSpawnPoints = new();
        public HashSet<Vector2> FloorDebrisSpawnPoints = new();
    }
}
