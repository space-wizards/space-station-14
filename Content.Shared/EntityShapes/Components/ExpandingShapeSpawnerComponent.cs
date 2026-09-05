using System.Numerics;
using Content.Shared.EntityShapes.Shapes;
using Robust.Shared.GameStates;

namespace Content.Shared.EntityShapes.Components;

/// <summary>
/// Spawns an entity shape periodically or with a delay. Can be modified to expand, shrink, or move with time.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ExpandingShapeSpawnerComponent : Component
{
    /// <summary>
    /// If specified, changes the <see cref="EntityShape.Offset"/> on each trigger by its amount.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2? CounterOffset;

    /// <summary>
    /// If specified, changes the <see cref="EntityShape.Size"/> on each trigger by its amount.
    /// </summary>
    /// <remarks>
    /// This is intentionally a float, so you can specify fractions
    /// and make it grow only on each second, third, etc. activation.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public float? CounterSize;

    /// <summary>
    /// If specified, changes the <see cref="EntityShape.StepSize"/> on each trigger by its amount.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float? CounterStepSize;
}
