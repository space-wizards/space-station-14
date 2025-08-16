using Content.Server.Connection.Whitelist;

namespace Content.Server._NullLink;

public sealed partial class NullLinkRolesCondition : WhitelistCondition
{
    [DataField]
    public List<ulong> Roles = [];
}