using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration
{
    [Serializable, NetSerializable]
    public record PlayerInfo(
        string Username,
        string CharacterName,
        string IdentityName,
        string StartingJob,
        bool Antag,
        NetEntity? NetEntity,
        NetUserId SessionId,
        bool Connected,
        bool ActiveThisRound,
        TimeSpan? OverallPlaytime)
    {
        public string PlaytimeString()
        {
            return OverallPlaytime?.ToString("%d':'hh':'mm") ?? Loc.GetString("generic-unknown-title");
        }
    }
}
