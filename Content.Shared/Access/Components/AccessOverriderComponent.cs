using Content.Shared.Access.Systems;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Access.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedAccessOverriderSystem))]
public sealed partial class AccessOverriderComponent : Component
{
    public static string PrivilegedIdCardSlotId = "AccessOverrider-privilegedId";

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
    public List<string> AccessLevels = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("doAfter")]
    public float DoAfterTime = 0f;

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
