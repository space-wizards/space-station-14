using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Will add stamina to an entity when triggered.
/// If TargetUser is true it the user will have stamina added instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InflictStaminaOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// Should the damage ignore resistances?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IgnoreResistances;

    /// <summary>
    /// The stamina amount that is added to the target.
    /// May be further modified by <see cref="Systems.BeforeDamageOnTriggerEvent"/> subscriptions.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float Stamina;
}
