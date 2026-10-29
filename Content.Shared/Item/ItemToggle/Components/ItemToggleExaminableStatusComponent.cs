using Robust.Shared.GameStates;

namespace Content.Shared.Item.ItemToggle.Components;

/// <summary>
/// With this component, <see cref="ItemToggleComponent"/> will show its status on examine.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ItemToggleSystem))]
public sealed partial class ItemToggleExaminableStatusComponent : Component
{
    /// <summary>
    /// The text to show if the item is toggled on.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId OnText = "item-toggle-examined-on";

    /// <summary>
    /// The text to show if the item is toggled off.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId OffText = "item-toggle-examined-off";
}
