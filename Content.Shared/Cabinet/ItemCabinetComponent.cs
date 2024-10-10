using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Cabinet;

/// <summary>
/// Used for entities that can be opened, closed, and can hold one item. E.g., fire extinguisher cabinets.
/// Requires <c>OpenableComponent</c>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ItemCabinetSystem))]
public sealed partial class ItemCabinetComponent : Component
{
    /// <summary>
    /// Name of the <see cref="ItemSlot"/> that stores the actual item.
    /// </summary>
    [DataField]
    public string Slot = "ItemCabinet";
}

[Serializable, NetSerializable]
public enum ItemCabinetVisuals : byte
{
    ContainsItem,
    Layer
}
