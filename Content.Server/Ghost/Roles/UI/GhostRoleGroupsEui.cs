using Content.Server.EUI;
using Content.Shared.Ghost.Roles;

namespace Content.Server.Ghost.Roles.UI;

public sealed class GhostRoleGroupsEui : BaseEui
{
    public override AdminGhostRolesEuiState GetNewState()
    {
        var manager = EntitySystem.Get<GhostRoleGroupSystem>();
        var entManager = IoCManager.Resolve<EntityManager>();

        return new AdminGhostRolesEuiState(manager.GetAdminGhostRoleGroupInfo(Player), entManager);
    }

    public override void Closed()
    {
        base.Closed();

        EntitySystem.Get<GhostRoleGroupSystem>().CloseEui(Player);
    }
}
