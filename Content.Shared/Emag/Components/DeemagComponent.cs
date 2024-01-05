using Robust.Shared.GameStates;

namespace Content.Shared.Emag.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DeemagComponent : Component
{
    /// <summary>
    /// Duration of the operation on the device.
    /// </summary>
    [DataField]
    public float Duration = 5;

    /// <summary>
    /// Will the item be consumed when used.
    /// </summary>
    [DataField]
    public bool Consumable = true;
}
