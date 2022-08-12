using Robust.Shared.Map;

namespace Content.Shared.ShipGuns.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed class GunnerComponent : Component
{
    [ViewVariables]
    public SharedTargetingConsoleComponent? Console { get; set; }

    /// <summary>
    /// Where we started using the guns to check if we should break from moving too far.
    /// </summary>
    [ViewVariables]
    public EntityCoordinates? Position { set; get; }

    public const float BreakDistance = 0.25f;
}
