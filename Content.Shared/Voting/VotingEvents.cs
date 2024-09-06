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
    public VotePlayerListResponseEvent((NetUserId, string)[] players)
    {
        Players = players;
    }

    public (NetUserId, string)[] Players { get; }
}
