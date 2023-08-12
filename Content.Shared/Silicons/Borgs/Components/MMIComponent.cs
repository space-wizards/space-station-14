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
public sealed class MMIComponent : Component
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

    [DataField("hasMindState")]
    public string HasMindState = "mmi_alive";

    [DataField("noMindState")]
    public string NoMindState = "mmi_dead";

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
