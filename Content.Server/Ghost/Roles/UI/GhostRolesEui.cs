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
            var manager = IoCManager.Resolve<GhostRoleManager>();
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
                    IoCManager.Resolve<GhostRoleManager>().TakeoverImmediate(Player, req.Identifier);
                    break;
                case GhostRoleLotteryRequestMessage req:
                    IoCManager.Resolve<GhostRoleManager>().AddGhostRoleLotteryRequest(Player, req.Identifier);
                    break;
                case GhostRoleCancelLotteryRequestMessage req:
                    IoCManager.Resolve<GhostRoleManager>().RemoveGhostRoleLotteryRequest(Player, req.Identifier);
                    break;
                case GhostRoleFollowRequestMessage req:
                    EntitySystem.Get<GhostRoleSystem>().Follow(Player, req.Identifier);
                    break;
                case GhostRoleGroupLotteryRequestMessage req:
                    IoCManager.Resolve<GhostRoleManager>().AddRoleGroupLotteryRequest(Player, req.Identifier);
                    break;
                case GhostRoleGroupCancelLotteryMessage req:
                    IoCManager.Resolve<GhostRoleManager>().RemoveRoleGroupLotteryRequest(Player, req.Identifier);
                    break;
                case GhostRoleWindowCloseMessage _:
                    Closed();
                    break;
            }
        }

        public override void Closed()
        {
            base.Closed();

            EntitySystem.Get<GhostRoleSystem>().CloseEui(Player);
        }
    }
}
