using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;

namespace Content.Server.Ghost.Roles.UI
{
    public sealed class GhostRolesEui : BaseEui
    {
        public override GhostRolesEuiState GetNewState()
        {
            var manager = EntitySystem.Get<GhostRoleLotterySystem>();
            var adminManager = IoCManager.Resolve<IAdminManager>();

            return new GhostRolesEuiState(
                manager.GetGhostRoleGroupsInfo(Player),
                manager.GetGhostRolesInfo(Player),
                manager.LotteryStartTime,
                manager.LotteryExpiresTime,
                adminManager.IsAdmin(Player));
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            switch (msg)
            {
                case GhostRoleTakeoverRequestMessage req:
                    EntitySystem.Get<GhostRoleSystem>().RequestTakeover(Player, req.Identifier);
                    break;
                case GhostRoleFollowRequestMessage req:
                    EntitySystem.Get<GhostRoleSystem>().Follow(Player, req.Identifier);
                    break;
                case GhostRoleLotteryRequestMessage req:
                    EntitySystem.Get<GhostRoleLotterySystem>().GhostRoleAddPlayerLotteryRequest(Player, req.Identifier);
                    break;
                case GhostRoleCancelLotteryRequestMessage req:
                    EntitySystem.Get<GhostRoleLotterySystem>().GhostRoleRemovePlayerLotteryRequest(Player, req.Identifier);
                    break;
                case GhostRoleGroupLotteryRequestMessage req:
                    EntitySystem.Get<GhostRoleLotterySystem>().GhostRoleGroupAddPlayerLotteryRequest(Player, req.Identifier);
                    break;
                case GhostRoleGroupCancelLotteryMessage req:
                    EntitySystem.Get<GhostRoleLotterySystem>().GhostRoleGroupRemovePlayerLotteryRequest(Player, req.Identifier);
                    break;
                case GhostRoleWindowCloseMessage _:
                    Closed();
                    break;
            }
        }

        public override void Closed()
        {
            base.Closed();

            EntitySystem.Get<GhostRoleLotterySystem>().CloseEui(Player);
        }
    }
}
