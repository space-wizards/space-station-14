using Content.Server.Worldgen.Floorplanners;

namespace Content.Server.Worldgen.Systems;

public interface IFloorplanSystem
{
    public bool ConstructTiling(FloorplanConfig config, EntityUid targetGrid, Vector2 centerPoint, Constraint? bounds, out object? planData);

    public void Populate(FloorplanConfig config, EntityUid targetGrid, Vector2 centerPoint, Constraint? bounds, in object? planData);
}
