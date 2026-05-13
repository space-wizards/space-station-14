using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Ghost.Roles
{
    [Serializable, NetSerializable]
    public sealed partial class MakeGhostRoleEuiState : EuiStateBase
    {
        public MakeGhostRoleEuiState(NetEntity entity)
        {
            Entity = entity;
        }

        public NetEntity Entity { get; }
    }
}

