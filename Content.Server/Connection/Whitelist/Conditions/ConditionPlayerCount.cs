using System.Threading.Tasks;
using Robust.Server.Player;
using Robust.Shared.Network;

namespace Content.Server.Connection.Whitelist.Conditions;

public sealed partial class ConditionPlayerCount : WhitelistCondition
{
    public override async Task<(bool, string)> Condition(NetUserData data)
    {
        var plyManager = IoCManager.Resolve<IPlayerManager>();
        var count = plyManager.PlayerCount;
        return (count >= MinimumPlayers && count <= MaximumPlayers, Loc.GetString("whitelist-player-count"));
    }

    public int MinimumPlayers  = 0;
    public int MaximumPlayers = int.MaxValue;
}
