using Content.Shared.Access.Systems;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Access.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedAccessOverriderSystem))]
public sealed class AccessOverriderComponent : Component
{
    public static string PrivilegedIdCardSlotId = "IdCardConsole-privilegedId";

    [DataField("privilegedIdSlot")]
    public ItemSlot PrivilegedIdSlot = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("denialSound")]
    public SoundSpecifier? DenialSound;

    public EntityUid TargetAccessReaderId = new();

    [Serializable, NetSerializable]
    public sealed class WriteToTargetAccessReaderIdMessage : BoundUserInterfaceMessage
    {
        public readonly List<string> AccessList;

        public WriteToTargetAccessReaderIdMessage(List<string> accessList)
        {
            AccessList = accessList;
        }
    }

    [DataField("accessLevels", customTypeSerializer: typeof(PrototypeIdListSerializer<AccessLevelPrototype>))]
    public List<string> AccessLevels = new()
    {
        "Armory",
        "Atmospherics",
        "Bar",
        "Brig",
        "Detective",
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
    public sealed class AccessOverriderBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string PrivilegedIdName;
        public readonly bool IsPrivilegedIdPresent;
        public readonly bool IsPrivilegedIdAuthorized;
        public readonly string[]? TargetAccessReaderIdAccessList;
        public readonly string[]? AllowedModifyAccessList;

        public AccessOverriderBoundUserInterfaceState(bool isPrivilegedIdPresent,
            bool isPrivilegedIdAuthorized,
            string[]? targetAccessReaderIdAccessList,
            string[]? allowedModifyAccessList,
            string privilegedIdName)
        {
            IsPrivilegedIdPresent = isPrivilegedIdPresent;
            IsPrivilegedIdAuthorized = isPrivilegedIdAuthorized;
            TargetAccessReaderIdAccessList = targetAccessReaderIdAccessList;
            AllowedModifyAccessList = allowedModifyAccessList;
            PrivilegedIdName = privilegedIdName;
        }
    }

    [Serializable, NetSerializable]
    public enum AccessOverriderUiKey : byte
    {
        Key,
    }
}
