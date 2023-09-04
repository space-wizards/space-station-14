using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
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
    [DataField("brainSlotId")]
    public string BrainSlotId = "brain_slot";

    /// <summary>
    /// The <see cref="ItemSlot"/> for this implanter
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public ItemSlot BrainSlot = default!;

    /// <summary>
    /// The sprite state when the brain inserted has a mind.
    /// </summary>
    [DataField("hasMindState")]
    public string HasMindState = "mmi_alive";

    /// <summary>
    /// The sprite state when the brain inserted doesn't have a mind.
    /// </summary>
    [DataField("noMindState")]
    public string NoMindState = "mmi_dead";

    /// <summary>
    /// The sprite state when there is no brain inserted.
    /// </summary>
    [DataField("noBrainState")]
    public string NoBrainState = "mmi_off";
}

[Serializable, NetSerializable]
public enum MMIVisuals : byte
{
    BrainPresent,
    HasMind
}

[Serializable, NetSerializable]
public enum MMIVisualLayers : byte
{
    Brain,
    Base
}
