using Content.Shared.Ghost.Roles.Raffles;
using Robust.Shared.Player;

namespace Content.Shared.Ghost.Roles.Components;

/// <summary>
/// Indicates that a ghost role is currently being raffled, and stores data about the raffle in progress.
/// Raffles start when the first player joins a raffle.
/// </summary>
[RegisterComponent]
[Access(typeof(SharedGhostRoleSystem))]
public sealed partial class GhostRoleRaffleComponent : Component
{
    /// <summary>
    /// Identifier of the <see cref="GhostRoleComponent">Ghost Role</see> this raffle is for.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public uint Identifier { get; set; }

    /// <summary>
    /// List of sessions that are currently in the raffle.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<ICommonSession> CurrentMembers = [];

    /// <summary>
    /// List of sessions that are currently or were previously in the raffle.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<ICommonSession> AllMembers = [];

    /// <summary>
    /// Time left in the raffle in seconds. This must be initialized to a positive value.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan Countdown = TimeSpan.MaxValue;

    /// <summary>
    /// The cumulative time, i.e. how much time the raffle will take in total. Added to when the time is extended
    /// by someone joining the raffle.
    /// Must be set to the same value as <see cref="Countdown"/> on initialization.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan CumulativeTime = TimeSpan.MaxValue;

    /// <inheritdoc cref="GhostRoleRaffleSettings.JoinExtendsDurationBy"/>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan JoinExtendsDurationBy { get; set; }

    /// <inheritdoc cref="GhostRoleRaffleSettings.MaxDuration"/>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan MaxDuration { get; set; }
}
