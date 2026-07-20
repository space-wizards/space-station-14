using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.EntityShapes.Components;

/// <summary>
/// Spawns an entity shape periodically or with a delay. Can be modified to expand, shrink, or move with time.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ExpandingShapeSpawnerComponent : Component
{
    [DataField, AutoNetworkedField]
    public Vector2? CounterOffset;

    [DataField, AutoNetworkedField]
    public float? CounterSize;

    [DataField, AutoNetworkedField]
    public float? CounterStepSize;
}
