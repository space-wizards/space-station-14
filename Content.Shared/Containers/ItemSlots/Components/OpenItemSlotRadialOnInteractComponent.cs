using Robust.Shared.GameStates;

namespace Content.Shared.Containers.ItemSlots.Components;

/// <summary>
/// Interacting with this entity will open up a radial menu displaying every occupied item slot.
/// Clicking on them ejects them. This requires the <seealso cref="ItemSlotsComponent"/> on the entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class OpenItemSlotRadialOnInteractComponent : Component
{
    /// <summary>
    /// Whether the radial menu opens up on normal interactions or alternative interactions.
    /// </summary>
    [DataField]
    public bool AltInteraction;
}
