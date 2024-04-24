using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration
{
    [Serializable, NetSerializable]
    public sealed record PlayerInfo(
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
        private string? _playtimeString;

        public string PlaytimeString => _playtimeString ??=
            OverallPlaytime?.ToString("%d':'hh':'mm") ?? Loc.GetString("generic-unknown-title");

        public bool Equals(PlayerInfo? other)
        {
            return other?.SessionId == SessionId;
        }

        public override int GetHashCode()
        {
            return SessionId.GetHashCode();
        }
    }
}
