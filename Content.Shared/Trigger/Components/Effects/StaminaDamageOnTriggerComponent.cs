using Content.Shared.Trigger.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Will inflict stamina to an entity when triggered.
/// If TargetUser is true it the user will have stamina inflicted instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StaminaDamageOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// Should the inflicted stamina ignore resistances?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IgnoreResistances;

    /// <summary>
    /// The stamina amount that is inflicted to the target.
    /// May be further modified by <see cref="BeforeStaminaDamageOnTriggerEvent"/> subscriptions.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float Stamina;
}
