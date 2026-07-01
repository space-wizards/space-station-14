using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Shared.Pointing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class PointingArrowComponent : Component
{
    /// <summary>
    /// The position of the sender when the point began.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public Vector2 StartPosition;

    /// <summary>
    /// When the pointing arrow ends
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan EndTime;

    /// <summary>
    /// Whether this arrow should become rogue when its normal lifetime ends.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public bool Rogue;
}
