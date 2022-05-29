using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Ghost.Roles
{
    [Serializable, NetSerializable]
    public sealed class MakeGhostRoleEuiState : EuiStateBase
    {
        public MakeGhostRoleEuiState(EntityUid entityUid)
        {
            EntityUid = entityUid;
        }

        public EntityUid EntityUid { get; }
    }
}
