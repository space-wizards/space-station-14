using Robust.Shared.GameStates;

namespace Content.Shared.NameModifier.Components;

/// <summary>
/// Used to manage modifiers on an entity's name and handle renaming in a way
/// that survives being renamed by multiple systems.
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NameModifierComponent : Component
{
    /// <summary>
    /// The entity's name without any modifiers.
    /// If you want to base a modifier on the entity's name, use this so that modifiers
    /// aren't duplicated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string BaseName = string.Empty;
}
