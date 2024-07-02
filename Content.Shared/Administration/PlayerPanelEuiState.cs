using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

[Serializable, NetSerializable]
public sealed class PlayerPanelEuiState(string username, TimeSpan playtime, int? totalNotes, int? totalBans, int? totalRoleBans, bool? whitelisted)
    : EuiStateBase
{
    public readonly string Username = username;
    public readonly TimeSpan Playtime = playtime;
    public readonly int? TotalNotes = totalNotes;
    public readonly int? TotalBans = totalBans;
    public readonly int? TotalRoleBans = totalRoleBans;
    public readonly bool? Whitelisted = whitelisted;
}
