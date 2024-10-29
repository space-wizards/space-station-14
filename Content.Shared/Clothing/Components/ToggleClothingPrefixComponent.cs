using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

/// <summary>
/// Handles the changes to ClothingComponent.EquippedPrefix when toggled.
/// </summary>
/// <remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ToggleClothingPrefixComponent : Component
{
    /// <summary>
    ///     Clothing's EquippedPrefix when activated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? PrefixOn = "on";

    /// <summary>
    ///     Clothing's EquippedPrefix when deactivated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? PrefixOff;
}
