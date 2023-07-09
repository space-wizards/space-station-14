using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;

namespace Content.Server.Ghost.Roles.UI
{
    public sealed class MakeGhostRoleEui : BaseEui
    {
        public MakeGhostRoleEui(EntityUid entityUid)
        {
            EntityUid = entityUid;
        }

        public EntityUid EntityUid { get; }

        public override EuiStateBase GetNewState()
        {
            return new MakeGhostRoleEuiState(EntityUid);
        }

        public override void Closed()
        {
            base.Closed();

            EntitySystem.Get<GhostRoleSystem>().CloseMakeGhostRoleEui(Player);
        }
    }
}
