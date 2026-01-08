using Robust.Shared.GameStates;

namespace Content.Shared.Research.Components;

/// <summary>
///     Entity that accumulates R&D research points passively over time.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ResearchPointSourceComponent : Component
{
    /// <summary>
    ///     Points accumulated per second.
    /// </summary>
    [DataField("pointspersecond")]
    public int PointsPerSecond;

    /// <summary>
    ///     If this source is actively accumulating points.
    /// </summary>
    [DataField]
    public bool Active;
}
