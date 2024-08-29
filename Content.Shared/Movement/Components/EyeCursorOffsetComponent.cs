using System.Numerics;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Displaces SS14 eye data when given to an entity.
/// </summary>
[RegisterComponent] //Needs access restriction
public sealed partial class EyeCursorOffsetComponent : Component
{
    public Vector2 TargetPosition = Vector2.Zero;

    public Vector2 CurrentPosition = Vector2.Zero;

    /// <summary>
    /// The amount the view will be displaced when the cursor is positioned at/beyond the max offset distance.
    /// Measured in tiles.
    /// </summary>
    [DataField]
    public float MaxOffset = 3f;

    /// <summary>
    /// The speed which the camera adjusts to new positions. 0.5f seems like a good value, but can be changed if you want very slow/instant adjustments.
    /// </summary>
    [DataField]
    public float OffsetSpeed = 0.5f;
}
