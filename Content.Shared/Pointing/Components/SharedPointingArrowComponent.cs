using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Shared.Pointing.Components;

[NetworkedComponent]
public abstract partial class SharedPointingArrowComponent : Component
{
    /// <summary>
    /// The position of the sender when the point began.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 StartPosition;

    /// <summary>
    /// When the pointing arrow ends
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EndTime;
}
