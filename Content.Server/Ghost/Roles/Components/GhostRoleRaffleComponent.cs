using Content.Server.Ghost.Roles.Raffles;
using Robust.Shared.Player;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Ghost.Roles.Components;

/// <summary>
/// Indicates that a ghost role is currently being raffled, and stores data about the raffle in progress.
/// Raffles start when the first player joins a raffle.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(GhostRoleSystem))]
public sealed partial class GhostRoleRaffleComponent : Component
{
    /// <summary>
    /// Identifier of the <see cref="GhostRoleComponent">Ghost Role</see> this raffle is for.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
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
    /// Absolute simulation time at which the raffle ends.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan EndTime;

    /// <summary>
    /// The cumulative time, i.e. how much time the raffle will take in total. Added to when the time is extended
    /// by someone joining the raffle.
    /// Must be initialized to the raffle's configured initial duration.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("cumulativeTime")]
    public TimeSpan CumulativeTime = TimeSpan.MaxValue;

    /// <inheritdoc cref="GhostRoleRaffleSettings.JoinExtendsDurationBy"/>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("joinExtendsDurationBy")]
    public TimeSpan JoinExtendsDurationBy { get; set; }

    /// <inheritdoc cref="GhostRoleRaffleSettings.MaxDuration"/>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("maxDuration")]
    public TimeSpan MaxDuration { get; set; }
}
