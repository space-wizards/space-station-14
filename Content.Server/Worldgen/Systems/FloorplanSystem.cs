using Content.Server.Worldgen.Floorplanners;

namespace Content.Server.Worldgen.Systems;

public abstract class FloorplanSystem : EntitySystem
{
    public abstract bool ConstructTiling(FloorplanConfig config, EntityUid targetGrid, Vector2 centerPoint, Constraint? bounds, out object? planData);

    public abstract void Populate(FloorplanConfig config, EntityUid targetGrid, Vector2 centerPoint, Constraint? bounds, in object? planData);
}
