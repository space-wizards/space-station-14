using System.ComponentModel.DataAnnotations;
using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Guardian.Components;

/// <summary>
/// An entity with this status effect will apply <see cref="SelfEffects"/> to itself and <see cref="VictimEffects"/> to its victim when picked up or collided with (being picked up is actually detected via <see cref="ContactInteractionEvent"/> so this will be fired via interactions with things such as lockers too).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GuardianTrappedStatusEffectComponent : Component
{
    /// <summary>
    /// Effects that will be applied to the entity with this component
    /// </summary>
    [DataField]
    public EntityEffect[]? SelfEffects;

    /// <summary>
    /// Effects that will be applied to the entity that triggered the trap
    /// </summary>
    [DataField]
    public EntityEffect[]? VictimEffects;

    /// <summary>
    /// This status effect's prototype. Needed so that we may remove the status effect.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId SelfPrototype;
}
