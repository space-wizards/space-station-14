using System.Threading.Tasks;
using Robust.Server.Player;
using Robust.Shared.Network;

namespace Content.Server.Connection.Whitelist.Conditions;

public sealed partial class ConditionPlayerCount : WhitelistCondition
{
    public override Task<bool> Condition(NetUserData data)
    {
        var plyManager = IoCManager.Resolve<IPlayerManager>();
        var count = plyManager.PlayerCount;
        return Task.FromResult(count >= MinimumPlayers && count <= MaximumPlayers);
    }

    public int MinimumPlayers  = 0;
    public int MaximumPlayers = int.MaxValue;

    public override string DenyMessage { get; }
}
