using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Client.DirectionalArrowIndicator;

/// <summary>
/// Component that manages directional arrow indicators that spawn clientside when an entity is examined.
/// Handled by <see cref="DirectionalArrowIndicatorSystem"/>
/// </summary>
[RegisterComponent]
public sealed partial class DirectionalArrowIndicatorComponent : Component
{
    /// <summary>
    /// The lifetime of the arrow indicator in seconds.
    /// </summary>
    [DataField]
    public float Lifetime = 2f;

    /// <summary>
    /// List of arrows to spawn.
    /// </summary>
    [DataField]
    public List<ArrowSpawnData> Arrows = new();
}

/// <summary>
/// Data for spawning a single directional arrow.
/// </summary>
[DataDefinition]
public sealed partial class ArrowSpawnData
{
    /// <summary>
    /// Offset relative to entity's position where the arrow should appear.
    /// </summary>
    [DataField]
    public Vector2 Offset = Vector2.Zero;

    /// <summary>
    /// Rotation of the arrow.
    /// </summary>
    [DataField]
    public Angle Rotation = Angle.Zero;

    /// <summary>
    /// Prototype ID of the arrow entity to spawn.
    /// </summary>
    [DataField]
    public EntProtoId ArrowType = "RedDirectionalArrowIndicator";
}
