using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Holds SS14 eye data not relevant for engine, e.g. lerp targets.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class ContentEyeComponent : Component
{
    /// <summary>
    /// Zoom we're lerping to.
    /// </summary>
    [DataField("targetZoom")]
    public Vector2 TargetZoom = Vector2.One;

    /// <summary>
    /// How far we're allowed to zoom out.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxZoom")]
    public Vector2 MaxZoom = Vector2.One;
}
