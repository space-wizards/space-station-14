#nullable enable
using System;
using Content.Shared.Eui;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Observer.GhostRoles
{
    [Serializable, NetSerializable]
    public class MakeGhostRoleEuiState : EuiStateBase
    {
        public MakeGhostRoleEuiState(EntityUid entityUid)
        {
            EntityUid = entityUid;
        }

        public EntityUid EntityUid { get; }
    }
}
