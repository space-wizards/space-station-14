using Robust.Shared.Network;

namespace Content.Server.Connection.Whitelist.Conditions;

public sealed partial class AlwaysDeny : WhitelistCondition
{
    public override bool Condition(NetUserData data)
    {
        return false;
    }

    public override string DenyMessage { get; } = "whitelist-always-deny";
}
