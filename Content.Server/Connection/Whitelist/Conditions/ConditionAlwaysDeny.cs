using System.Threading.Tasks;
using Robust.Shared.Network;

namespace Content.Server.Connection.Whitelist.Conditions;

public sealed partial class ConditionAlwaysDeny : WhitelistCondition
{
    public override async Task<bool> Condition(NetUserData data)
    {
        return false;
    }

    public override string DenyMessage { get; } = "whitelist-always-deny";
}
