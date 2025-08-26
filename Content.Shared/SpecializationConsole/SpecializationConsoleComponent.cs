using Content.Shared.Containers.ItemSlots;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SpecializationConsole;

/// <summary>
/// Attached to entities that can set data on linked turret-based entities
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SpecializationConsoleComponent : Component
{
    public static string PrivilegedIdCardSlotId = "SpecializationConsole-privilegedId";
    public static string TargetIdCardSlotId = "SpecializationConsole-targetId";

    [DataField]
    public ItemSlot PrivilegedIdSlot = new();

    [DataField]
    public ItemSlot TargetIdSlot = new();

    public HumanoidCharacterProfile? Profile;


    /// <summary>
    /// Sound to play when denying access to the device.
    /// </summary>
    [DataField]
    public SoundSpecifier AccessDeniedSound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");
}

[Serializable, NetSerializable]
public enum SpecializationConsoleWindowUiKey : byte
{
    Key,
}


[Serializable, NetSerializable]
public sealed class SpecializationConsoleBoundInterfaceState : BoundUserInterfaceState
{
    public readonly bool IsPrivilegedIdPresent;
    public readonly bool IsTargetIdPresent;
    public readonly string? TargetIdJobTitle;
    public readonly string? TargetIdFullName;
    public readonly string? TargetIdJobSpec;
    public readonly HumanoidCharacterProfile? Profile;
    public readonly ProtoId<JobPrototype>? Job;

    public SpecializationConsoleBoundInterfaceState(bool isPrivilegedIdPresent,
        bool isTargetIdPresent,
        string? targetIdFullName,
        string? targetIdJobTitle,
        string? targetIdJobSpec,
        HumanoidCharacterProfile? profile,
        ProtoId<JobPrototype>? job)
    {
        IsPrivilegedIdPresent = isPrivilegedIdPresent;
        IsTargetIdPresent = isTargetIdPresent;
        TargetIdFullName = targetIdFullName;
        TargetIdJobTitle = targetIdJobTitle;
        TargetIdJobSpec = targetIdJobSpec;
        Profile = profile;
        Job = job;
    }
}

[Serializable, NetSerializable]
public sealed class NewEmployeeDataEvent : BoundUserInterfaceMessage
{
}
