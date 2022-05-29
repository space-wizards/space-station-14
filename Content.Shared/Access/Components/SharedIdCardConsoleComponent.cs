using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Access.Components
{
    [NetworkedComponent]
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

        // Put this on shared so we just send the state once in PVS range rather than every time the UI updates.

        [ViewVariables]
        [DataField("accessLevels", customTypeSerializer: typeof(PrototypeIdListSerializer<AccessLevelPrototype>))]
        public List<string> AccessLevels = new()
        {
            "Armory",
            "Atmospherics",
            "Bar",
            "Brig",
            // "Detective",
            "Captain",
            "Cargo",
            "Chapel",
            "Chemistry",
            "ChiefEngineer",
            "ChiefMedicalOfficer",
            "Command",
            "Engineering",
            "External",
            "HeadOfPersonnel",
            "HeadOfSecurity",
            "Hydroponics",
            "Janitor",
            "Kitchen",
            "Maintenance",
            "Medical",
            "Quartermaster",
            "Research",
            "ResearchDirector",
            "Salvage",
            "Security",
            "Service",
            "Theatre",
        };

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
                string[]? targetIdAccessList,
                string privilegedIdName,
                string targetIdName)
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
        public enum IdCardConsoleUiKey : byte
        {
            Key,
        }
    }
}
