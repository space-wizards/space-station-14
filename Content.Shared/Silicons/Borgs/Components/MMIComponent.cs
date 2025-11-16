using Content.Shared.Containers.ItemSlots;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// This is used for an entity that takes a brain
/// in an item slot before transferring consciousness.
/// Used for borg stuff.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedBorgSystem))]
public sealed partial class MMIComponent : Component
{
    /// <summary>
    /// The ID of the itemslot that holds the brain.
    /// </summary>
    [DataField]
    public string BrainSlotId = "brain_slot";

    /// <summary>
    /// The <see cref="ItemSlot"/> for this implanter
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public ItemSlot BrainSlot = default!;

    /// <summary>
    /// The sprite state when the brain inserted has a mind.
    /// </summary>
    [DataField]
    public string HasMindState = "mmi_alive";

    /// <summary>
    /// The sprite state when the brain inserted doesn't have a mind.
    /// </summary>
    [DataField]
    public string NoMindState = "mmi_dead";

    /// <summary>
    /// The sprite state when the brain inserted doesn't have a mind and is searching for a ghost role.
    /// </summary>
    [DataField]
    public string SearchingMindState = "mmi_searching";

    /// <summary>
    /// The sprite state when there is no brain inserted.
    /// </summary>
    [DataField]
    public string NoBrainState = "mmi_off";

    /// <summary>
    /// If true, a brain without mind being inserted into this MMI creates a ghost role (similar to a positronic brain).
    /// </summary>
    [DataField]
    public bool EnableGhostRole;

    /// <summary>
    /// The name shown on the Ghost Role list
    /// </summary>
    [DataField]
    public LocId RoleName = "mmi-brain-role-name";

    /// <summary>
    /// The description shown on the Ghost Role list
    /// </summary>
    [DataField]
    public LocId RoleDescription = "mmi-brain-role-description";

    /// <summary>
    /// The introductory message shown when trying to take the ghost role/join the raffle
    /// </summary>
    [DataField]
    public LocId RoleRules = "ghost-role-information-silicon-rules";

    /// <summary>
    /// A list of mind roles that will be added to the entity's mind
    /// </summary>
    [DataField]
    public List<EntProtoId> MindRoles = new() { "MindRoleGhostRoleSilicon" };

    /// <summary>
    /// The prototype ID of the job that will be given to the controlling mind
    /// </summary>
    [DataField]
    public ProtoId<JobPrototype>? JobProto = "Borg";
}

[Serializable, NetSerializable]
public enum MMIVisuals : byte
{
    BrainPresent,
    MindState,
}

[Serializable, NetSerializable]
public enum MMIVisualLayers : byte
{
    Brain,
    Base,
}

[Serializable, NetSerializable]
public enum MMIVisualsMindstate : byte
{
    NoMind,
    Searching,
    HasMind,
}

