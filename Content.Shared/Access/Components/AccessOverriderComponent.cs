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

    [Serializable, NetSerializable]
    public sealed class WriteToTargetAccessReaderIdMessage : BoundUserInterfaceMessage
    {
        public readonly List<string> AccessList;

        public WriteToTargetAccessReaderIdMessage(List<string> accessList)
        {
            AccessList = accessList;
        }
    }

    [DataField, AutoNetworkedField(true)]
    public List<ProtoId<AccessLevelPrototype>> AccessLevels = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float DoAfter;

    [Serializable, NetSerializable]
    public sealed class AccessOverriderBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string TargetLabel;
        public readonly Color TargetLabelColor;
        public readonly string PrivilegedIdName;
        public readonly bool IsPrivilegedIdPresent;
        public readonly bool IsPrivilegedIdAuthorized;
        public readonly string[]? TargetAccessReaderIdAccessList;
        public readonly string[]? AllowedModifyAccessList;
        public readonly string[]? MissingPrivilegesList;

        public AccessOverriderBoundUserInterfaceState(bool isPrivilegedIdPresent,
            bool isPrivilegedIdAuthorized,
            string[]? targetAccessReaderIdAccessList,
            string[]? allowedModifyAccessList,
            string[]? missingPrivilegesList,
            string privilegedIdName,
            string targetLabel,
            Color targetLabelColor)
        {
            IsPrivilegedIdPresent = isPrivilegedIdPresent;
            IsPrivilegedIdAuthorized = isPrivilegedIdAuthorized;
            TargetAccessReaderIdAccessList = targetAccessReaderIdAccessList;
            AllowedModifyAccessList = allowedModifyAccessList;
            MissingPrivilegesList = missingPrivilegesList;
            PrivilegedIdName = privilegedIdName;
            TargetLabel = targetLabel;
            TargetLabelColor = targetLabelColor;
        }
    }

    [Serializable, NetSerializable]
    public enum AccessOverriderUiKey : byte
    {
        Key,
    }
}
