using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;

namespace Content.Server.Ghost.Roles.UI
{
    public sealed class MakeGhostRoleEui : BaseEui
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public MakeGhostRoleEui(EntityUid entityUid)
        {
            EntityUid = entityUid;
        }

        public EntityUid EntityUid { get; }

        public override EuiStateBase GetNewState()
        {
            return new MakeGhostRoleEuiState(_entManager.GetNetEntity(EntityUid));
        }

        public override void Closed()
        {
            base.Closed();

            _entManager.System<GhostRoleSystem>().CloseMakeGhostRoleEui(Player);
        }
    }
}
