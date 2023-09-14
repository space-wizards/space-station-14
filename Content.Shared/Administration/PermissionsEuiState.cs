using Content.Shared.Eui;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration
{
    [Serializable, NetSerializable]
    public sealed class PermissionsEuiState : EuiStateBase
    {
        public bool IsLoading;

        public AdminData[] Admins = Array.Empty<AdminData>();
        public Dictionary<int, AdminRankData> AdminRanks = new();

        [Serializable, NetSerializable]
        public struct AdminData
        {
            public NetUserId UserId;
            public string? UserName;
            public string? Title;
            public AdminFlags PosFlags;
            public AdminFlags NegFlags;
            public int? RankId;
        }

        [Serializable, NetSerializable]
        public struct AdminRankData
        {
            public string Name;
            public AdminFlags Flags;
        }
    }

    public static class PermissionsEuiMsg
    {
        [Serializable, NetSerializable]
        public sealed class AddAdmin : EuiMessageBase
        {
            public string UserNameOrId = string.Empty;
            public string? Title;
            public AdminFlags PosFlags;
            public AdminFlags NegFlags;
            public int? RankId;
        }

        [Serializable, NetSerializable]
        public sealed class RemoveAdmin : EuiMessageBase
        {
            public NetUserId UserId;
        }

        [Serializable, NetSerializable]
        public sealed class UpdateAdmin : EuiMessageBase
        {
            public NetUserId UserId;
            public string? Title;
            public AdminFlags PosFlags;
            public AdminFlags NegFlags;
            public int? RankId;
        }


        [Serializable, NetSerializable]
        public sealed class AddAdminRank : EuiMessageBase
        {
            public string Name = string.Empty;
            public AdminFlags Flags;
        }

        [Serializable, NetSerializable]
        public sealed class RemoveAdminRank : EuiMessageBase
        {
            public int Id;
        }

        [Serializable, NetSerializable]
        public sealed class UpdateAdminRank : EuiMessageBase
        {
            public int Id;

            public string Name = string.Empty;
            public AdminFlags Flags;
        }
    }
}
