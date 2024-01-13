using System.Threading.Tasks;
using Robust.Shared.Network;

namespace Content.Server.Connection.Whitelist.Conditions;

public sealed partial class ConditionAlwaysDeny : WhitelistCondition
{
    public override async Task<(bool, string)> Condition(NetUserData data)
    {
        return (false, Loc.GetString("whitelist-always-deny"));
    }
}
