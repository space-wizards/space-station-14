using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Ghost.Roles
{
    [NetSerializable, Serializable]
    public struct GhostRoleInfo
    {
        public string Identifier { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Rules { get; set; }
        public int AvailableLotteryRoleCount { get; set; }
        public int AvailableImmediateRoleCount { get; set; }
    }

    [NetSerializable, Serializable]
    public struct GhostRoleGroupInfo
    {
        public uint Identifier { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public int AvailableCount { get; set; }
    }

    [NetSerializable, Serializable]
    public sealed class GhostRolesEuiState : EuiStateBase
    {
        public GhostRoleGroupInfo[] GhostRoleGroups { get; }
        public GhostRoleInfo[] GhostRoles { get; }
        public string[] PlayerRequests { get; }
        public uint [] PlayerRoleGroupRequests { get; }
        public TimeSpan LotteryStart { get; }
        public TimeSpan LotteryEnd { get; }
        public bool ShowAdminControls { get; }

        public GhostRolesEuiState(GhostRoleGroupInfo[] ghostRoleGroups, GhostRoleInfo[] ghostRoles, string[] requests, uint[] groupRequests, TimeSpan lotteryStart, TimeSpan lotteryEnd, bool showAdminControls)
        {
            GhostRoleGroups = ghostRoleGroups;
            GhostRoles = ghostRoles;
            PlayerRequests = requests;
            PlayerRoleGroupRequests = groupRequests;
            LotteryStart = lotteryStart;
            LotteryEnd = lotteryEnd;
            ShowAdminControls = showAdminControls;
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
    public sealed class GhostRoleLotteryRequestMessage : EuiMessageBase
    {
        public string Identifier { get; }

        public GhostRoleLotteryRequestMessage(string identifier)
        {
            Identifier = identifier;
        }
    }

    [NetSerializable, Serializable]
    public sealed class GhostRoleCancelLotteryRequestMessage : EuiMessageBase
    {
        public string Identifier { get; }

        public GhostRoleCancelLotteryRequestMessage(string identifier)
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
    public sealed class GhostRoleGroupLotteryRequestMessage : EuiMessageBase
    {
        public uint Identifier { get; }

        public GhostRoleGroupLotteryRequestMessage(uint identifier)
        {
            Identifier = identifier;
        }
    }

    [NetSerializable, Serializable]
    public sealed class GhostRoleGroupCancelLotteryMessage : EuiMessageBase
    {
        public uint Identifier { get; }

        public GhostRoleGroupCancelLotteryMessage(uint identifier)
        {
            Identifier = identifier;
        }
    }

    [NetSerializable, Serializable]
    public sealed class GhostRoleWindowCloseMessage : EuiMessageBase
    {
    }
}
