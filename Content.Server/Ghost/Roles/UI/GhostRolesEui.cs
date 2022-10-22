using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;

namespace Content.Server.Ghost.Roles.UI
{
    public sealed class GhostRolesEui : BaseEui
    {
        [Dependency] private readonly IEntitySystemManager _sysMan = default!;
        public override GhostRolesEuiState GetNewState()
        {
            return new(_sysMan.GetEntitySystem<GhostRoleSystem>().GetGhostRolesInfo());
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            switch (msg)
            {
                case GhostRoleTakeoverRequestMessage req:
                    _sysMan.GetEntitySystem<GhostRoleSystem>().Takeover(Player, req.Identifier);
                    break;
                case GhostRoleFollowRequestMessage req:
                    _sysMan.GetEntitySystem<GhostRoleSystem>().Follow(Player, req.Identifier);
                    break;
                case GhostRoleWindowCloseMessage _:
                    Closed();
                    break;
            }
        }

        public override void Closed()
        {
            base.Closed();

            _sysMan.GetEntitySystem<GhostRoleSystem>().CloseEui(Player);
        }
    }
}
