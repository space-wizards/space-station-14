using Content.Server.Worldgen.Components;
using Content.Server.Worldgen.Floorplanners;
using Robust.Shared.Map;

namespace Content.Server.Worldgen.Systems.Planes;

/// <summary>
/// This handles loading floorplans when their range is entered.
/// This uses planes as to avoid a more complex method of checking for overlaps.
/// </summary>
public sealed class FloorplanningPlaneSystem : WorldChunkPlaneSystem<FloorplanningChunk, FloorplanningConfig>
{
    public override LoadingFlags LoadingMask => LoadingFlags.Player; // Don't want mass scanners loading large areas in full

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PlannedAreaComponent, ComponentShutdown>(OnPlanRemoved);
        SubscribeLocalEvent<PlannedAreaComponent, MoveEvent>(OnPlanMoved);
    }

    private void OnPlanMoved(EntityUid uid, PlannedAreaComponent component, ref MoveEvent args)
    {
        var mapGrid = Comp<IMapGridComponent>(uid);
        foreach (var chunk in component.OwningChunks)
        {
            var chunkData = GetChunk(mapGrid.Grid.ParentMapId, chunk);
            chunkData.PlannedAreas.Remove(uid);
        }

        foreach (var plan in component.Plans)
        {
            var potentialChunk = WorldSpaceToChunkSpace(mapGrid.Grid.LocalToWorld(plan.GridPosition));
            var chunk = GetChunk(mapGrid.Grid.ParentMapId, potentialChunk.Floored());
            chunk.PlannedAreas.Add(uid);
        }
    }

    private void OnPlanRemoved(EntityUid uid, PlannedAreaComponent component, ComponentShutdown args)
    {
        var mapGrid = Comp<IMapGridComponent>(uid);

        foreach (var plan in component.Plans)
        {
            var potentialChunk = WorldSpaceToChunkSpace(mapGrid.Grid.LocalToWorld(plan.GridPosition));
            var chunk = GetChunk(mapGrid.Grid.ParentMapId, potentialChunk.Floored());
            chunk.PlannedAreas.Remove(uid);
        }
    }

    protected override void LoadChunk(MapId map, Vector2i chunk)
    {
        var chunkData = GetChunk(map, chunk);
        foreach (var plannedEntity in chunkData.PlannedAreas)
        {
            var gridComp = Comp<IMapGridComponent>(plannedEntity);
            var plans = Comp<PlannedAreaComponent>(plannedEntity);
            Logger.Debug($"Populating {plannedEntity}.");
            for (var i = 0; i < plans.Plans.Count; i++)
            {
                var plan = plans.Plans[i];

                if (!(WorldSpaceToChunkSpace(gridComp.Grid.LocalToWorld(plan.GridPosition)).Floored() == chunk))
                    continue;

                plans.Plans.RemoveAt(i);
                i -= 1;
                plan.Config.Populate(plannedEntity, plan.GridPosition, null, EntityManager.EntitySysManager, plan.PlanData);
            }

            plans.OwningChunks.Remove(chunk);
        }
    }

    protected override void UnloadChunk(MapId map, Vector2i chunk)
    {

    }

    public bool ConstructTiling(FloorplanConfig config, EntityUid targetGrid, Vector2 gridPosition, Constraint? bounds)
    {
        if (!TryComp<IMapGridComponent>(targetGrid, out var gridComp))
            throw new ArgumentException($"Tried to do floorplanning on a non-grid, {ToPrettyString(targetGrid)}!");

        var planned = EnsureComp<PlannedAreaComponent>(targetGrid);
        var success = config.ConstructTiling(targetGrid, gridPosition, bounds, EntityManager.EntitySysManager, out var planData);
        if (!success)
            return false;

        var chunk = WorldSpaceToChunkSpace(gridComp.Grid.LocalToWorld(gridPosition)).Floored();

        if (LoadedChunks[gridComp.Grid.ParentMapId].Contains(chunk))
        {
            config.Populate(targetGrid, gridPosition, bounds, EntityManager.EntitySysManager, in planData);
            return true;
        }

        planned.Plans.Add(new Plan(config, planData, gridPosition));

        var chunkData = GetChunk(gridComp.Grid.ParentMapId, chunk);
        Logger.Debug("Setting up ");
        chunkData.PlannedAreas.Add(targetGrid);
        planned.OwningChunks.Add(chunk);

        return true;
    }

    protected override FloorplanningChunk InitializeChunk(MapId map, Vector2i chunk)
    {
        return new FloorplanningChunk();
    }

    public override bool TryClearWorldSpace(Box2Rotated area)
    {
        // Do nothing.
        return true;
    }

    public override bool TryClearWorldSpace(Circle area)
    {
        // Do nothing.
        return true;
    }
}

public struct FloorplanningConfig
{

}

public sealed class FloorplanningChunk
{
    public HashSet<EntityUid> PlannedAreas = new();
}
