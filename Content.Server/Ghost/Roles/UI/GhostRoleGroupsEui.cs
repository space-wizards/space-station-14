using Content.Server.EUI;
using Content.Shared.Ghost.Roles;

namespace Content.Server.Ghost.Roles.UI;

public sealed class GhostRoleGroupsEui : BaseEui
{
    public override AdminGhostRolesEuiState GetNewState()
    {
        var manager = IoCManager.Resolve<GhostRoleManager>();

        return new AdminGhostRolesEuiState(manager.GetAdminGhostRoleGroupInfo(Player));
    }

    public override void Closed()
    {
        base.Closed();

        EntitySystem.Get<GhostRoleGroupSystem>().CloseEui(Player);
    }
}
