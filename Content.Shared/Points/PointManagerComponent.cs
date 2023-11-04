using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared.Points;

/// <summary>
/// This is a component that generically stores points for all players.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedPointSystem))]
public sealed partial class PointManagerComponent : Component
{
    /// <summary>
    /// A dictionary of a player's netuserID to the amount of points they have.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<NetUserId, FixedPoint2> Points = new();

    /// <summary>
    /// A text-only version of the scoreboard used by the client.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FormattedMessage Scoreboard = new();
}
