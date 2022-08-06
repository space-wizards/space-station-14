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
            return new GhostRolesEuiState(manager.GetGhostRolesInfo(Player), manager.LotteryStartTime, manager.LotteryExpiresTime);
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
                    IoCManager.Resolve<GhostRoleManager>().AddPlayerRequest(Player, req.Identifier);
                    break;
                case GhostRoleCancelLotteryRequestMessage req:
                    IoCManager.Resolve<GhostRoleManager>().RemovePlayerRequest(Player, req.Identifier);
                    break;
                case GhostRoleFollowRequestMessage req:
                    EntitySystem.Get<GhostRoleSystem>().Follow(Player, req.Identifier);
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
