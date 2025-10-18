using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Voting;

[Serializable, NetSerializable]
public sealed class VotePlayerListRequestEvent : EntityEventArgs
{

}

[Serializable, NetSerializable]
public sealed class VotePlayerListResponseEvent : EntityEventArgs
{
    public VotePlayerListResponseEvent((NetUserId, NetEntity, string)[] players, bool denied)
    {
        Players = players;
        Denied = denied;
    }

    /// <summary>
    /// The players available to have a votekick started for them.
    /// </summary>
    public (NetUserId, NetEntity, string)[] Players { get; }

    /// <summary>
    /// Whether the server will allow the user to start a votekick or not.
    /// </summary>
    public bool Denied;
}
