using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Points;

/// <summary>
/// This is a component that generically stores points for all players.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PointManagerComponent : Component
{
    /// <summary>
    /// A dictionary of a player's netuserID to the amount of points they have.
    /// </summary>
    [DataField("points")]
    public Dictionary<NetUserId, FixedPoint2> Points = new();
}

[Serializable, NetSerializable]
public sealed class PointManagerComponentState : ComponentState
{
    public Dictionary<NetUserId, FixedPoint2> Points;

    public PointManagerComponentState(Dictionary<NetUserId, FixedPoint2> points)
    {
        Points = points;
    }
}
