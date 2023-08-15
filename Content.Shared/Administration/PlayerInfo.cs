using Content.Shared.Objectives;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration
{
    [Serializable, NetSerializable]
    public sealed class RequestObjectivesEvent : EntityEventArgs
    {
        public readonly NetUserId NetUserId;

        public RequestObjectivesEvent(NetUserId netUserId)
        {
            NetUserId = netUserId;
        }
    }

    [Serializable, NetSerializable]
    public record PlayerInfo(
        string Username,
        string CharacterName,
        string IdentityName,
        string StartingJob,
        bool Antag,
        Dictionary<string, List<ConditionInfo>> Objectives,
        EntityUid? EntityUid,
        NetUserId SessionId,
        bool Connected,
        bool ActiveThisRound);
}
