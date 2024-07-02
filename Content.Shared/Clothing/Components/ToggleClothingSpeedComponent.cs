using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.Components;

/// <summary>
/// This is used for a clothing item that gives a speed modification that is toggleable.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ClothingSpeedModifierSystem)), AutoGenerateComponentState]
public sealed partial class ToggleClothingSpeedComponent : Component
{
    /// <summary>
    /// The action for toggling the clothing.
    /// </summary>
    [DataField]
    public EntProtoId ToggleAction = "ActionToggleSpeedBoots";

    /// <summary>
    ///     Does this clothing item have a sprite that should update when it is activated?
    ///     e.g. speedboots have a red region that changes to green when they are active
    /// </summary>
    [DataField] public bool ToggleableSprite = false;

    /// <summary>
    /// The action entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;

    /// <summary>
    /// The state of the toggle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled;
}

public sealed partial class ToggleClothingSpeedEvent : InstantActionEvent
{

}
