using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;

namespace Content.Server.Ghost.Roles.UI
{
    public sealed class GhostRolesEui : BaseEui
    {
        public override GhostRolesEuiState GetNewState()
        {
            var system = EntitySystem.Get<GhostRoleSystem>();
            return new(system.GetGhostRolesInfo(Player), system.LotteryStartTime, system.LotteryExpiresTime);
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            switch (msg)
            {
                case GhostRoleTakeoverRequestMessage req:
                    EntitySystem.Get<GhostRoleSystem>().TakeoverImmediate(Player, req.Identifier);
                    break;
                case GhostRoleLotteryRequestMessage req:
                    EntitySystem.Get<GhostRoleSystem>().AddToRoleLottery(Player, req.Identifier);
                    break;
                case GhostRoleCancelLotteryRequestMessage req:
                    EntitySystem.Get<GhostRoleSystem>().RemoveFromRoleLottery(Player, req.Identifier);
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
