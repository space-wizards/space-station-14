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
    [ViewVariables]
    public Vector2 TargetZoom = Vector2.One;
}
