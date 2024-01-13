using System.Threading.Tasks;
using Content.Server.Database;
using Robust.Shared.Network;

namespace Content.Server.Connection.Whitelist.Conditions;

public sealed partial class ConditionManualBlacklist : WhitelistCondition
{
    public override async Task<(bool, string)> Condition(NetUserData data)
    {
        var db = IoCManager.Resolve<IServerDbManager>();
        return (!(await db.GetBlacklistStatusAsync(data.UserId)), Loc.GetString("whitelist-blacklisted"));
    }
}
