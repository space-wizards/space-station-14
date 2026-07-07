using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Ghost.Roles;

/// <summary>
/// State for the "waiting for the party to be ready" dialog shown to players
/// who have claimed a slot in a ghost role party.
/// </summary>
[Serializable, NetSerializable]
public sealed class GhostRolePartyWaitingEuiState : EuiStateBase
{
    public int Ready;
    public int Total;
}

/// <summary>
/// Sent by the client when the player abandons their claimed party slot,
/// returning them to ghost and re-opening the ghost role.
/// </summary>
[Serializable, NetSerializable]
public sealed class GhostRolePartyCancelMessage : EuiMessageBase;
