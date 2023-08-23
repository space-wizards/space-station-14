using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Cabinet;

/// <summary>
///     Used for entities that can be opened, closed, and can hold one item. E.g., fire extinguisher cabinets.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ItemCabinetComponent : Component
{
    /// <summary>
    ///     Sound to be played when the cabinet door is opened.
    /// </summary>
    [DataField("doorSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? DoorSound;

    /// <summary>
    ///     The <see cref="ItemSlot"/> that stores the actual item. The entity whitelist, sounds, and other
    ///     behaviours are specified by this <see cref="ItemSlot"/> definition.
    /// </summary>
    [DataField("cabinetSlot"), ViewVariables]
    public ItemSlot CabinetSlot = new();

    /// <summary>
    ///     Whether the cabinet is currently open or not.
    /// </summary>
    [DataField("opened")]
    public bool Opened;

    /// <summary>
    /// The state for when the cabinet is open
    /// </summary>
    [DataField("openState"), ViewVariables(VVAccess.ReadWrite)]
    public string? OpenState;

    /// <summary>
    /// The state for when the cabinet is closed
    /// </summary>
    [DataField("closedState"), ViewVariables(VVAccess.ReadWrite)]
    public string? ClosedState;
}

[Serializable, NetSerializable]
public sealed class ItemCabinetComponentState : ComponentState
{
    public SoundSpecifier? DoorSound;

    public bool Opened;

    public string? OpenState;

    public string? ClosedState;

    public ItemCabinetComponentState(SoundSpecifier? doorSound, bool opened, string? openState, string? closedState)
    {
        DoorSound = doorSound;
        Opened = opened;
        OpenState = openState;
        ClosedState = closedState;
    }
}

