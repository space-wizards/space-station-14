using Content.Shared.Eui;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Ghost.Roles;

/// <summary>
/// All information for a ghost role.
/// </summary>
/// <remarks>
/// Passed to a client when querying available ghost roles.
/// </remarks>
[NetSerializable, Serializable]
public struct GhostRoleInfo
{
    /// <summary>
    /// The ghost entity.
    /// </summary>
    public NetEntity Identifier { get; set; }

    /// <summary>
    /// The localized name of the role.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The localized description of the role.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// The localized rules for the role.
    /// </summary>
    public string Rules { get; set; }

    /// <summary>
    /// A list of all antag and job prototype IDs of the ghost role and its mind role(s).
    /// </summary>
    public (List<ProtoId<JobPrototype>>?, List<ProtoId<AntagPrototype>>?) RolePrototypes;

    /// <inheritdoc cref="GhostRoleKind"/>
    public GhostRoleKind Kind { get; set; }

    /// <summary>
    /// if <see cref="Kind"/> is <see cref="GhostRoleKind.RaffleInProgress"/>, specifies how many players are currently
    /// in the raffle for this role.
    /// </summary>
    public uint RafflePlayerCount { get; set; }

    /// <summary>
    /// if <see cref="Kind"/> is <see cref="GhostRoleKind.RaffleInProgress"/>, specifies when raffle finishes.
    /// </summary>
    public TimeSpan RaffleEndTime { get; set; }

}

/// <summary>
/// An update for the <see cref="GhostRolesEui"/>. Contains all available ghost roles.
/// </summary>
[NetSerializable, Serializable]
public sealed class GhostRolesEuiState(GhostRoleInfo[] ghostRoles) : EuiStateBase
{
    /// <summary>
    /// The currently available ghost roles that can be taken by the player.
    /// </summary>
    public GhostRoleInfo[] GhostRoles { get; } = ghostRoles;
}

/// <summary>
/// A message to take over a particular ghost role.
/// </summary>
[NetSerializable, Serializable]
public sealed class RequestGhostRoleMessage(NetEntity identifier) : EuiMessageBase
{
    /// <summary>
    /// The entity of the ghost role to take.
    /// </summary>
    public NetEntity Identifier { get; } = identifier;
}

/// <summary>
/// A message to start following a particular ghost role.
/// </summary>
[NetSerializable, Serializable]
public sealed class FollowGhostRoleMessage(NetEntity identifier) : EuiMessageBase
{
    /// <summary>
    /// The entity to follow.
    /// </summary>
    public NetEntity Identifier { get; } = identifier;
}

/// <summary>
/// A message to leave the raffle for a ghost role.
/// </summary>
[NetSerializable, Serializable]
public sealed class LeaveGhostRoleRaffleMessage(NetEntity identifier) : EuiMessageBase
{
    /// <summary>
    /// The raffle to leave.
    /// </summary>
    public NetEntity Identifier { get; } = identifier;
}

/// <summary>
/// Determines whether a ghost role is a raffle role, and if it is, whether it's running.
/// </summary>
[NetSerializable, Serializable]
public enum GhostRoleKind
{
    /// <summary>
    /// Role is not a raffle role and can be taken immediately.
    /// </summary>
    FirstComeFirstServe,

    /// <summary>
    /// Role is a raffle role, but raffle hasn't started yet.
    /// </summary>
    RaffleReady,

    /// <summary>
    ///  Role is raffle role and currently being raffled, but player hasn't joined raffle.
    /// </summary>
    RaffleInProgress,

    /// <summary>
    /// Role is raffle role and currently being raffled, and player joined raffle.
    /// </summary>
    RaffleJoined
}

