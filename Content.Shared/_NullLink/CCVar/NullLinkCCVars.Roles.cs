using Robust.Shared.Configuration;

namespace Content.Shared.NullLink.CCVar;
public sealed partial class NullLinkCCVars
{
    // U’ve got some confusion because both the Discord role and the in-game role (like Captain, for example) are called “role.”

    public static readonly CVarDef<string> RoleReqWithAccessToAllRoles =
        CVarDef.Create("nulllink.roles_req.all-roles", "AllRolesReq", CVar.SERVER | CVar.REPLICATED);
}
