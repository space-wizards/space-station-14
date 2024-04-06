using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Containers.ItemSlots;

/// <summary>
/// Fills an entity's itemslots at MapInit.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ItemSlotsFillSystem))]
public sealed partial class ItemSlotsFillComponent : Component
{
    /// <summary>
    /// The item to spawn for each slot.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, EntProtoId> Items = new();
}
