using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Serialization;

namespace Content.Shared.Access.Components
{
    public abstract class SharedIdCardConsoleComponent : Component
    {
        public const int MaxFullNameLength = 256;
        public const int MaxJobTitleLength = 256;

        public static string PrivilegedIdCardSlotId = "IdCardConsole-privilegedId";
        public static string TargetIdCardSlotId = "IdCardConsole-targetId";

        [DataField("privilegedIdSlot")]
        public ItemSlot PrivilegedIdSlot = new();

        [DataField("targetIdSlot")]
        public ItemSlot TargetIdSlot = new();

        [Serializable, NetSerializable]
        public sealed class WriteToTargetIdMessage : BoundUserInterfaceMessage
        {
            public readonly string FullName;
            public readonly string JobTitle;
            public readonly List<string> AccessList;

            public WriteToTargetIdMessage(string fullName, string jobTitle, List<string> accessList)
            {
                FullName = fullName;
                JobTitle = jobTitle;
                AccessList = accessList;
            }
        }

        [Serializable, NetSerializable]
        public sealed class IdCardConsoleBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly string PrivilegedIdName;
            public readonly bool IsPrivilegedIdPresent;
            public readonly bool IsPrivilegedIdAuthorized;
            public readonly bool IsTargetIdPresent;
            public readonly string TargetIdName;
            public readonly string? TargetIdFullName;
            public readonly string? TargetIdJobTitle;
            public readonly string[]? TargetIdAccessList;

            public IdCardConsoleBoundUserInterfaceState(bool isPrivilegedIdPresent,
                bool isPrivilegedIdAuthorized,
                bool isTargetIdPresent,
                string? targetIdFullName,
                string? targetIdJobTitle,
                string[]? targetIdAccessList, string privilegedIdName, string targetIdName)
            {
                IsPrivilegedIdPresent = isPrivilegedIdPresent;
                IsPrivilegedIdAuthorized = isPrivilegedIdAuthorized;
                IsTargetIdPresent = isTargetIdPresent;
                TargetIdFullName = targetIdFullName;
                TargetIdJobTitle = targetIdJobTitle;
                TargetIdAccessList = targetIdAccessList;
                PrivilegedIdName = privilegedIdName;
                TargetIdName = targetIdName;
            }
        }

        [Serializable, NetSerializable]
        public enum IdCardConsoleUiKey
        {
            Key,
        }
    }
}
