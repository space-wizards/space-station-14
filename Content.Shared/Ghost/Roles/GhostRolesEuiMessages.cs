using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Ghost.Roles
{
    [NetSerializable, Serializable]
    public struct GhostRoleInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Rules { get; set; }
        public TimeSpan ExpiresAt { get; set; }
        public TimeSpan AddedAt { get; set; }
        public bool IsRequested { get; set; }
    }

    [NetSerializable, Serializable]
    public sealed class GhostRolesEuiState : EuiStateBase
    {
        public GhostRoleInfo[] GhostRoles { get; }

        public GhostRolesEuiState(GhostRoleInfo[] ghostRoles)
        {
            GhostRoles = ghostRoles;
        }
    }

    [NetSerializable, Serializable]
    public sealed class GhostRoleTakeoverRequestMessage : EuiMessageBase
    {
        public string Identifier { get; }

        public GhostRoleTakeoverRequestMessage(string identifier)
        {
            Identifier = identifier;
        }
    }

    [NetSerializable, Serializable]
    public sealed class GhostRoleCancelTakeoverRequestMessage : EuiMessageBase
    {
        public string Identifier { get; }

        public GhostRoleCancelTakeoverRequestMessage(string identifier)
        {
            Identifier = identifier;
        }
    }

    [NetSerializable, Serializable]
    public sealed class GhostRoleFollowRequestMessage : EuiMessageBase
    {
        public string Identifier { get; }

        public GhostRoleFollowRequestMessage(string identifier)
        {
            Identifier = identifier;
        }
    }

    [NetSerializable, Serializable]
    public sealed class GhostRoleWindowCloseMessage : EuiMessageBase
    {
    }
}
