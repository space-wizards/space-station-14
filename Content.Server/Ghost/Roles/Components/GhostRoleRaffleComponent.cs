using Robust.Shared.Player;

namespace Content.Server.Ghost.Roles.Components;

/// <summary>
/// Indicates that a ghost role is currently being raffled, and stores data about the raffle in progress.
/// Raffles start when the first player joins a raffle.
/// </summary>
[RegisterComponent]
[Access(typeof(GhostRoleSystem))]
public sealed partial class GhostRoleRaffleComponent : Component
{
    /// <summary>
    /// Identifier of the <see cref="GhostRoleComponent">Ghost Role</see> this raffle is for.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public uint Identifier { get; set; }

    /// <summary>
    /// List of sessions that are currently in the raffle.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public readonly HashSet<ICommonSession> CurrentMembers = [];

    /// <summary>
    /// List of sessions that are currently or were previously in the raffle.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public readonly HashSet<ICommonSession> AllMembers = [];

    /// <summary>
    /// Time left in the raffle in seconds. This must be initialized to a positive value.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float Countdown = float.NaN;

    /// <summary>
    /// The cumulative time, i.e. how much time the raffle will take in total. Added to when the time is extended
    /// by someone joining the raffle.
    /// Must be set to the same value as <see cref="Countdown"/> on initialization.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float CumulativeTime = float.NaN;
}
