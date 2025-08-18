using Robust.Shared.GameStates;

namespace Content.Shared.VendingMachines.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class VendingMachineContrabandComponent : Component
{
    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, VendingMachineInventoryEntry> ContrabandInventory = [];
}
