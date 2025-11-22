using Content.Shared.Containers.ItemSlots;
using Content.Shared.Ghost.Roles;
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
    /// Must have <see cref="GhostRole"/> set to work.
    /// </summary>
    [DataField]
    public bool EnableGhostRole;

    /// <summary>
    /// If true, the brain required to create a ghost role must have had a player inhabit it at some point.
    /// </summary>
    [DataField]
    public bool GhostRoleRequiresPlayerBrain = true;

    /// <summary>
    /// If true, the brain required to create a ghost role must have had a player inhabit it at some point.
    /// </summary>
    [DataField]
    public GhostRoleSettings? GhostRole;
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
    Coloration,
}

[Serializable, NetSerializable]
public enum MMIVisualsMindstate : byte
{
    NoMind,
    Searching,
    HasMind,
}

