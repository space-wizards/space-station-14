using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.BanList;

[Serializable, NetSerializable]
public sealed class BanListEuiState : EuiStateBase
{
    public BanListEuiState(string banListPlayerName, List<SharedServerBan> bans)
    {
        BanListPlayerName = banListPlayerName;
        Bans = bans;
    }

    public string BanListPlayerName { get; }
    public List<SharedServerBan> Bans { get; }
}
