using System.Threading.Tasks;
using Robust.Server.Player;
using Robust.Shared.Network;

namespace Content.Server.Connection.Whitelist.Conditions;

public sealed partial class ConditionPlayerCount : WhitelistCondition
{
    [DataField]
    public int MinimumPlayers  = 0;
    [DataField]
    public int MaximumPlayers = int.MaxValue;
}
