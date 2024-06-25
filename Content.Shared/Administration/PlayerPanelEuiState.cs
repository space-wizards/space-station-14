using Content.Shared.Eui;

namespace Content.Shared.Administration;

public sealed class PlayerPanelEuiState(string username, uint totalNotes, uint totalBans, bool whitelisted)
    : EuiStateBase
{
    public readonly string Username = username;
    public readonly uint? TotalNotes = totalNotes;
    public readonly uint? TotalBans = totalBans;
    public readonly bool? Whitelisted = whitelisted;
}
