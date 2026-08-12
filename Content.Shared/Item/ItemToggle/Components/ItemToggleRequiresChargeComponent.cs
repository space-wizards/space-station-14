using Robust.Shared.GameStates;

namespace Content.Shared.Item.ItemToggle.Components;

/// <summary>
/// With this component, <see cref="ItemToggleComponent"/> will not toggle and will be toggled off automatically if battery charge is below the required value.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ItemToggleSystem))]
public sealed partial class ItemToggleRequiresChargeComponent : Component
{
    /// <summary>
    /// The amount of battery charge required to function.
    /// </summary>
    [DataField(required: true)]
    public float RequiredCharge;

    /// <summary>
    /// The popup to show if someone tries to toggle the item on, but there is not enough charge.
    /// </summary>
    [DataField]
    public LocId FailPopup = "item-toggle-activated-low-charge";
}
