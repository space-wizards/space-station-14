using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.EntitySystemMessages
{
    [NetSerializable, Serializable]
    public struct GhostRoleInfo
    {
        public uint Identifier { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    [NetSerializable, Serializable]
    public class GhostRoleMessage : EntitySystemMessage
    {
    }

    [NetSerializable, Serializable]
    public class GhostRoleUpdateRequestMessage : GhostRoleMessage
    {
    }

    [NetSerializable, Serializable]
    public class GhostRoleOutdatedMessage : GhostRoleMessage
    {
    }

    [NetSerializable, Serializable]
    public class GhostRoleUpdateMessage : GhostRoleMessage
    {
        private GhostRoleInfo[] _ghostRoles;

        public GhostRoleUpdateMessage(GhostRoleInfo[] ghostRoles)
        {
            _ghostRoles = ghostRoles;
        }
    }

    [NetSerializable, Serializable]
    public class GhostRoleTakeoverRequestMessage : GhostRoleMessage
    {
        public uint Identifier { get; set; }
    }
}
