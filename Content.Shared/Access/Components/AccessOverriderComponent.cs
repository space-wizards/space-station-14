using Content.Shared.Access.Systems;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Access.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedAccessOverriderSystem))]
public sealed partial class AccessOverriderComponent : Component
{
    public static string PrivilegedIdCardSlotId = "AccessOverrider-privilegedId";

    [DataField]
    public ItemSlot PrivilegedIdSlot = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public SoundSpecifier? DenialSound;

    public EntityUid TargetAccessReaderId = new();

    // Starlight-edit: Start
    [DataField, AutoNetworkedField]
    public List<ProtoId<AccessGroupPrototype>> AccessGroups = new();

    [DataField, AutoNetworkedField]
    public ProtoId<AccessGroupPrototype>? CurrentAccessGroup;

    // Keep existing AccessLevels (backwards-compatible)
    [DataField, AutoNetworkedField]
    public List<ProtoId<AccessLevelPrototype>> AccessLevels = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float DoAfter;
    // Starlight-edit: End

    [Serializable, NetSerializable]
    public sealed class WriteToTargetAccessReaderIdMessage : BoundUserInterfaceMessage
    {
        public readonly List<ProtoId<AccessLevelPrototype>> AccessList;

        public WriteToTargetAccessReaderIdMessage(List<ProtoId<AccessLevelPrototype>> accessList)
        {
            AccessList = accessList;
        }
    }

    // Starlight-edit: Start
    [Serializable, NetSerializable]
    public sealed class AccessGroupSelectedMessage : BoundUserInterfaceMessage
    {
        public readonly ProtoId<AccessGroupPrototype> SelectedGroup;

        public AccessGroupSelectedMessage(ProtoId<AccessGroupPrototype> selectedGroup)
        {
            SelectedGroup = selectedGroup;
        }
    }
    // Starlight-edit: End

    [Serializable, NetSerializable]
    public sealed class AccessOverriderBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string TargetLabel;
        public readonly Color TargetLabelColor;
        public readonly string PrivilegedIdName;
        public readonly bool IsPrivilegedIdPresent;
        public readonly bool IsPrivilegedIdAuthorized;
        public readonly ProtoId<AccessLevelPrototype>[]? TargetAccessReaderIdAccessList;
        public readonly ProtoId<AccessLevelPrototype>[]? AllowedModifyAccessList;
        public readonly ProtoId<AccessLevelPrototype>[]? MissingPrivilegesList;

        // Starlight-edit: Start
        public readonly ProtoId<AccessGroupPrototype>[]? AccessGroups;
        public readonly ProtoId<AccessGroupPrototype>? CurrentAccessGroup;
        // Starlight-edit: End

        public AccessOverriderBoundUserInterfaceState(bool isPrivilegedIdPresent,
            bool isPrivilegedIdAuthorized,
            ProtoId<AccessLevelPrototype>[]? targetAccessReaderIdAccessList,
            ProtoId<AccessLevelPrototype>[]? allowedModifyAccessList,
            ProtoId<AccessLevelPrototype>[]? missingPrivilegesList,
            string privilegedIdName,
            string targetLabel,
            // Starlight-edit: Start
            Color targetLabelColor,
            ProtoId<AccessGroupPrototype>[]? accessGroups,
            ProtoId<AccessGroupPrototype>? currentAccessGroup)
            // Starlight-edit: End
        {
            IsPrivilegedIdPresent = isPrivilegedIdPresent;
            IsPrivilegedIdAuthorized = isPrivilegedIdAuthorized;
            TargetAccessReaderIdAccessList = targetAccessReaderIdAccessList;
            AllowedModifyAccessList = allowedModifyAccessList;
            MissingPrivilegesList = missingPrivilegesList;
            PrivilegedIdName = privilegedIdName;
            TargetLabel = targetLabel;
            TargetLabelColor = targetLabelColor;

            // Starlight-edit: Start
            AccessGroups = accessGroups;
            CurrentAccessGroup = currentAccessGroup;
            // Starlight-edit: End
        }
    }

    [Serializable, NetSerializable]
    public enum AccessOverriderUiKey : byte
    {
        Key,
    }
}
