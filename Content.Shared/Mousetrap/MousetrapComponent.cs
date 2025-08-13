using Robust.Shared.GameStates;

namespace Content.Shared.Mousetrap;

/// <summary>
/// Component inteded to be used for mouse traps.
/// Will stop step triggers from happening unless armed via <see cref="Item.ItemToggle.Components.ItemToggleComponent"/>
/// and will scale damage taken from <see cref="Trigger.Components.Effects.DamageOnTriggerComponent"/>
/// depending on mass.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MousetrapComponent : Component
{
    /// <summary>
    /// Set this to change where the
    /// inflection point in the damage scaling
    /// equation will occur.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MassBalance = 10;
}
