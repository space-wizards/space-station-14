using Content.Server.Eui;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Eui;
using Content.Shared.GameObjects.Components.Observer;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.Components.Observer
{
    public class GhostRolesEui : BaseEui
    {
        public override GhostRolesEuiState GetNewState()
        {
            return new(EntitySystem.Get<GhostRoleSystem>().GetGhostRolesInfo());
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            switch (msg)
            {
                case GhostRoleTakeoverRequestMessage req:
                    EntitySystem.Get<GhostRoleSystem>().Takeover(Player, req.Identifier);
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
