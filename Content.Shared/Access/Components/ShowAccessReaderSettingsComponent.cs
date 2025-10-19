using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.Access.Components;

/// <summary>
/// This component allows you to see whether an access reader's settings have been modified.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShowAccessReaderSettingsComponent : Component, IClothingSlots
{
    /// <summary>
    /// Determines from which equipment slots this entity can provide its benefits.
    /// </summary>
    public SlotFlags Slots { get; set; } = ~SlotFlags.POCKET;
}
