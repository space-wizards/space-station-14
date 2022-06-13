using Content.Server.Worldgen.Floorplanners;

namespace Content.Server.Worldgen.Components;

/// <summary>
/// This is used for objects areas that have an associated floorplan but have not been populated.
/// You'll see this most in planetary generation, but it's also used for space.
/// </summary>
[RegisterComponent]
public sealed class PlannedAreaComponent : Component
{
    public List<Plan> Plans = new();
    public HashSet<Vector2i> OwningChunks = new();
}

public record struct Plan(FloorplanConfig Config, object? PlanData, Vector2 GridPosition)
{
    public FloorplanConfig Config = Config;
    public object? PlanData = PlanData;
    public Vector2 GridPosition = GridPosition;
}
