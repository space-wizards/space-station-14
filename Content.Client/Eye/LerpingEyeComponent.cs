using System.Numerics;

namespace Content.Client.Eye;

/// <summary>
///     Component for keeping track of client-side eye lerping. This component should only be added or removed via the <see cref="EyeLerpingSystem"/>.
/// </summary>
[RegisterComponent]
public sealed partial class LerpingEyeComponent : Component
{
    /// <summary>
    ///     False if this eye was automatically added when a player was attached to this entity.
    /// </summary>
    public bool ManuallyAdded = false;

    /// <summary>
    /// Previous eye rotation relative to the movement reference entity, grid, or map.
    /// </summary>
    [ViewVariables]
    public Angle LastRotation;

    /// <summary>
    /// Target relative eye rotation. The reference entity's rendered rotation is applied separately.
    /// </summary>
    [ViewVariables]
    public Angle TargetRotation;

    /// <summary>
    /// Reference entity used by the saved relative rotations.
    /// </summary>
    [ViewVariables]
    public EntityUid? RotationReference;

    [ViewVariables]
    public Vector2 LastZoom;

    [ViewVariables]
    public Vector2 TargetZoom;
}
