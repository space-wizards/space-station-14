using System.Threading.Tasks;
using Content.Server.Database;
using Robust.Shared.Network;

namespace Content.Server.Connection.Whitelist.Conditions;

public sealed partial class ConditionManualWhitelist : WhitelistCondition
{
    public override async Task<bool> Condition(NetUserData data)
    {
        var db = IoCManager.Resolve<IServerDbManager>();
        return await db.GetWhitelistStatusAsync(data.UserId);
    }

    public override string DenyMessage { get; } = "whitelist-deny";
}
