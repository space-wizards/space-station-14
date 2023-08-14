using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Ghost.Roles
{
    [Serializable, NetSerializable]
    public sealed class MakeGhostRoleEuiState : EuiStateBase
    {
        public MakeGhostRoleEuiState(NetEntity entityUid)
        {
            EntityUid = entityUid;
        }

        public NetEntity EntityUid { get; }
    }
}
