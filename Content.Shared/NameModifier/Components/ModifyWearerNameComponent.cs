using Robust.Shared.GameStates;

namespace Content.Shared.NameModifier.Components;

/// <summary>
/// Adds a modifier to the wearer's name when this item is equipped,
/// and removes it when it is unequipped.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ModifyWearerNameComponent : Component
{
    /// <summary>
    /// The localization ID of the text to be used as the modifier.
    /// The base name will be passed in as <c>$baseName</c>
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId LocId = string.Empty;

    /// <summary>
    /// Priority of the modifier. See <see cref="EntitySystems.RefreshNameModifiersEvent"/> for more information.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Priority;
}
