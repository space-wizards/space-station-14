using Content.Server.Worldgen.Systems;

namespace Content.Server.Worldgen.Floorplanners;

[ImplicitDataDefinitionForInheritors]
public abstract record FloorplanConfig
{
    public abstract Type FloorplannerSystem { get; }

    public bool ConstructTiling(EntityUid targetGrid, Vector2 centerPoint, Constraint? bounds, IEntitySystemManager entitySystemManager, out object? planData)
    {
        var floorplanner = (IFloorplanSystem)entitySystemManager.GetEntitySystem(FloorplannerSystem);
        return floorplanner.ConstructTiling(this, targetGrid, centerPoint, bounds, out planData);
    }
}
