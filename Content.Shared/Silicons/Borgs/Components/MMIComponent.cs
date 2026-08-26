using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// This is used for an entity that takes a brain
/// in an item slot before transferring consciousness.
/// Used for borg stuff.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBorgSystem))]
public sealed partial class MMIComponent : Component
{
    /// <summary>
    /// The ID of the itemslot that holds the brain.
    /// </summary>
    [DataField]
    public string BrainSlotId = "brain_slot";

    /// <summary>
    /// The <see cref="ItemSlot"/> for this MMI. Holds the brain.
    /// </summary>
    [DataField(required: true)]
    public ItemSlot BrainSlot = new();

    /// <summary>
    /// The sprite state when the brain inserted has a mind.
    /// </summary>
    [DataField]
    public string HasMindState = "mmi_on";

    /// <summary>
    /// The sprite state when the brain inserted doesn't have a mind.
    /// </summary>
    [DataField]
    public string NoMindState = "mmi_on";

    /// <summary>
    /// The sprite state when there is no brain inserted.
    /// </summary>
    [DataField]
    public string NoBrainState = "mmi_off";

    /// <summary>
    /// The color of the <see cref="MMIVisualLayers.Unshaded"/> layer when the brain inserted has a mind.
    /// </summary>
    [DataField]
    public Color HasMindLightColor = Color.FromHex("#0094ff");

    /// <summary>
    /// The color of the <see cref="MMIVisualLayers.Unshaded"/> layer when the brain inserted doesn't have a mind.
    /// </summary>
    [DataField]
    public Color NoMindLightColor = Color.FromHex("#ff3033");
}

/// <summary>
/// AppearanceData keys for the MMI.
/// </summary>
[Serializable, NetSerializable]
public enum MMIVisuals : byte
{
    /// <summary>
    /// bool: Whether or not there is a brain in the MMI.
    /// </summary>
    BrainPresent,

    /// <summary>
    /// bool: Whether or not there is an active mind (a player) in the MMI.
    /// </summary>
    HasMind
}

/// <summary>
/// Sprite map keys for MMI visuals.
/// </summary>
[Serializable, NetSerializable]
public enum MMIVisualLayers : byte
{
    /// <summary>
    /// The layer of the brain.
    /// </summary>
    Brain,

    /// <summary>
    /// The layer of the housing.
    /// </summary>
    Base,

    /// <summary>
    /// The optional layer of an indicator light.
    /// </summary>
    Unshaded,
}
