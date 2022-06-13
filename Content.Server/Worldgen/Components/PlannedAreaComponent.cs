using Content.Server.Worldgen.Floorplanners;

namespace Content.Server.Worldgen.Components;

/// <summary>
/// This is used for objects areas that have an associated floorplan but have not been populated.
/// You'll see this most in planetary generation, but it's also used for space.
/// </summary>
[RegisterComponent]
public sealed class PlannedAreaComponent : Component
{
    public List<(FloorplanConfig, object?, Vector2 position)> Plans = new();
}

public record struct Plan
{
    public FloorplanConfig Config;
    public object? PlanData;
}
